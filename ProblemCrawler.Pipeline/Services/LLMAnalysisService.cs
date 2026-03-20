using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProblemCrawler.Pipeline.Clients;
using ProblemCrawler.Pipeline.Prompts;
using ProblemCrawler.Core.Configuration;
using ProblemCrawler.Core.Enums;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Core.Records.LLM;

namespace ProblemCrawler.Pipeline.Services;

public sealed class LLMAnalysisService(
    ICollectorItemRepository repository,
    OllamaHttpClient ollamaHttpClient,
    IOptions<LLMAnalysisConfiguration> analysisOptions,
    IOptions<OllamaConfiguration> ollamaOptions,
    ILogger<LLMAnalysisService> logger) : ILLMAnalysisService
{
    private readonly ICollectorItemRepository _repository = repository;
    private readonly OllamaHttpClient _ollamaHttpClient = ollamaHttpClient;
    private readonly LLMAnalysisConfiguration _analysisOptions = analysisOptions.Value;
    private readonly OllamaConfiguration _ollamaOptions = ollamaOptions.Value;
    private readonly ILogger<LLMAnalysisService> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> AllowedUrgencySignals = ["low", "medium", "high"];

    public async Task<LLMAnalysisRunSummary> ExecuteAsync(CancellationToken cancellationToken)
    {
        var evaluated = 0;
        var analysed = 0;
        var skipped = 0;
        var failed = 0;

        var batchSize = _analysisOptions.BatchSize <= 0 ? 100 : _analysisOptions.BatchSize;

        while (!cancellationToken.IsCancellationRequested)
        {
            var candidates = await _repository.GetLlmAnalysisCandidatesAsync(batchSize, cancellationToken);
            if (candidates.Count == 0)
            {
                break;
            }

            foreach (var candidate in candidates)
            {
                evaluated++;
                var executionResult = await ExecuteCandidateAsync(candidate, cancellationToken);
                if (executionResult.Success)
                {
                    analysed++;
                    continue;
                }

                if (executionResult.Attempts > 0)
                {
                    failed++;
                }
                else
                {
                    skipped++;
                }
            }

            if (candidates.Count < batchSize)
            {
                break;
            }
        }

        var summary = new LLMAnalysisRunSummary(evaluated, analysed, skipped, failed);

        _logger.LogInformation(
            "LLM analysis completed. Evaluated: {Evaluated}, analysed: {Analysed}, skipped: {Skipped}, failed: {Failed}",
            summary.Evaluated,
            summary.Analysed,
            summary.Skipped,
            summary.Failed);

        return summary;
    }

    public async Task<LLMAnalysisExecutionResult> ExecuteForItemAsync(Guid collectorItemId, CancellationToken cancellationToken)
    {
        var candidate = await _repository.GetLlmAnalysisCandidateByIdAsync(collectorItemId, cancellationToken);
        if (candidate is null)
        {
            return new LLMAnalysisExecutionResult(collectorItemId, false, 0, "Collector item was not found.", null);
        }

        if (candidate.CurrentStage != AnalysisStages.ReadyForAnalysis)
        {
            return new LLMAnalysisExecutionResult(
                collectorItemId,
                false,
                0,
                $"Collector item stage is {candidate.CurrentStage}; expected ReadyForAnalysis.",
                null);
        }

        return await ExecuteCandidateAsync(candidate, cancellationToken);
    }

    private async Task<LLMAnalysisExecutionResult> ExecuteCandidateAsync(LLMAnalysisCandidate candidate, CancellationToken cancellationToken)
    {
        var maxAttempts = Math.Max(1, _analysisOptions.MaxAttemptsPerItem);
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var context = await _repository.GetLlmAnalysisContextAsync(candidate.Id, cancellationToken);
                if (context is null)
                {
                    return new LLMAnalysisExecutionResult(candidate.Id, false, attempt, "Unable to load item context.", null);
                }

                var initialPrompt = LLMAnalysisPromptBuilder.BuildInitialPrompt(context);
                var modelOutput = await _ollamaHttpClient.GenerateAsync(initialPrompt, cancellationToken);
                if (string.IsNullOrWhiteSpace(modelOutput))
                {
                    continue;
                }

                var normalizedOutput = NormalizeModelOutput(modelOutput);

                if (TryParseResult(normalizedOutput, out var result, out var validationError))
                {
                    await PersistResultAsync(candidate.Id, result!, normalizedOutput, cancellationToken);
                    return new LLMAnalysisExecutionResult(candidate.Id, true, attempt, "Analysis succeeded.", result);
                }

                var repaired = await TryRepairResponseAsync(initialPrompt, normalizedOutput, validationError!, cancellationToken);
                if (repaired is not null)
                {
                    await PersistResultAsync(candidate.Id, repaired.Value.Result, repaired.Value.RawJson, cancellationToken);
                    return new LLMAnalysisExecutionResult(candidate.Id, true, attempt, "Analysis succeeded after response repair.", repaired.Value.Result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM analysis attempt {Attempt} failed for item {ItemId}", attempt, candidate.Id);
            }
        }

        return new LLMAnalysisExecutionResult(
            candidate.Id,
            false,
            maxAttempts,
            "All attempts exhausted for this execution; item remains ReadyForAnalysis.",
            null);
    }

    private async Task<(LLMAnalysisResult Result, string RawJson)?> TryRepairResponseAsync(
        string originalPrompt,
        string badResponse,
        string error,
        CancellationToken cancellationToken)
    {
        var maxRepairAttempts = Math.Max(1, _analysisOptions.MaxRepairAttempts);
        var previousResponse = badResponse;

        for (var repairAttempt = 1; repairAttempt <= maxRepairAttempts; repairAttempt++)
        {
            var repairPrompt = LLMAnalysisPromptBuilder.BuildRepairPrompt(originalPrompt, previousResponse, error);
            var repairedResponse = await _ollamaHttpClient.GenerateAsync(repairPrompt, cancellationToken);
            if (string.IsNullOrWhiteSpace(repairedResponse))
            {
                continue;
            }

            var normalized = NormalizeModelOutput(repairedResponse);
            if (TryParseResult(normalized, out var repairedResult, out _))
            {
                return (repairedResult!, normalized);
            }

            previousResponse = normalized;
        }

        return null;
    }

    private async Task PersistResultAsync(
        Guid collectorItemId,
        LLMAnalysisResult result,
        string rawJson,
        CancellationToken cancellationToken)
    {
        var upsert = new AnalysedItemUpsert(
            collectorItemId,
            result,
            rawJson,
            _ollamaOptions.Model,
            DateTime.UtcNow);

        await _repository.UpsertAnalysedItemAsync(upsert, cancellationToken);
    }

    private static bool TryParseResult(string rawResponse, out LLMAnalysisResult? result, out string? error)
    {
        result = null;
        error = null;

        try
        {
            result = JsonSerializer.Deserialize<LLMAnalysisResult>(rawResponse, JsonOptions);
        }
        catch (Exception ex)
        {
            error = $"JSON deserialization failed: {ex.Message}";
            return false;
        }

        if (result is null)
        {
            error = "Response payload was null.";
            return false;
        }

        if (result.PainLevel < 1 || result.PainLevel > 5)
        {
            error = "PainLevel must be in range [1..5].";
            return false;
        }

        if (result.Confidence < 0 || result.Confidence > 1)
        {
            error = "Confidence must be in range [0..1].";
            return false;
        }

        if (string.IsNullOrWhiteSpace(result.Industry))
        {
            error = "Industry is required and can be free text.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(result.UrgencySignal) || !AllowedUrgencySignals.Contains(result.UrgencySignal.ToLowerInvariant()))
        {
            error = "UrgencySignal must be one of: low, medium, high.";
            return false;
        }

        if (!result.ContainsProblem && result.IsActionable)
        {
            error = "IsActionable cannot be true when ContainsProblem is false.";
            return false;
        }

        return true;
    }

    private static string NormalizeModelOutput(string raw)
    {
        var trimmed = raw.Trim();

        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var lines = trimmed.Split('\n');
        if (lines.Length < 3)
        {
            return trimmed;
        }

        return string.Join('\n', lines.Skip(1).Take(lines.Length - 2)).Trim();
    }
}
