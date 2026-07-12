using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProblemCrawler.Core.Configuration;
using ProblemCrawler.Core.Enums;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Core.Records.LLM;
using ProblemCrawler.Pipeline.Clients;
using ProblemCrawler.Pipeline.Helper;
using ProblemCrawler.Pipeline.Prompts;
using System.Text.Json;

namespace ProblemCrawler.Pipeline.Services;

public sealed class LLMAnalysisService(
    ICollectorItemRepository repository,
    OllamaHttpClient ollamaHttpClient,
    IOptions<LLMAnalysisConfiguration> analysisOptions,
    IOptions<OllamaConfiguration> ollamaOptions,
    OllamaContextConfiguration contextConfiguration,
    OllamaJobGate ollamaJobGate,
    ILogger<LLMAnalysisService> logger) : ILLMAnalysisService
{
    
    private readonly ICollectorItemRepository _repository = repository;
    private readonly OllamaHttpClient _ollamaHttpClient = ollamaHttpClient;
    private readonly LLMAnalysisConfiguration _analysisOptions = analysisOptions.Value;
    private readonly OllamaConfiguration _ollamaOptions = ollamaOptions.Value;
    private readonly ILogger<LLMAnalysisService> _logger = logger;
    private readonly OllamaJobGate _ollamaJobGate = ollamaJobGate;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> AllowedUrgencySignals = ["low", "medium", "high"];

    public async Task<LLMAnalysisRunSummary> ExecuteAsync(CancellationToken cancellationToken)
    {
        
        await using var _ = await _ollamaJobGate.AcquireJobAsync(cancellationToken);
        var evaluated = 0;
        var analysed = 0;
        var skipped = 0;
        var failed = 0;

        var batchSize = _analysisOptions.BatchSize <= 0 ? 25 : _analysisOptions.BatchSize;

        while (!cancellationToken.IsCancellationRequested)
        {
            var candidates = await _repository.GetLlmAnalysisCandidatesAsync(batchSize, cancellationToken);
            if (candidates.Count == 0)
            {
                break;
            }
            var contexts = new Dictionary<Guid, LLMAnalysisContext>();
            foreach (var candidate in candidates)
            {
                var ctx = await _repository.GetLlmAnalysisContextAsync(candidate.Id, cancellationToken);
                if (ctx is not null) contexts[candidate.Id] = ctx;
            }

            var tasks = candidates.Select(async candidate =>
            {
                if (!contexts.TryGetValue(candidate.Id, out var context))
                    return new LLMAnalysisExecutionResult(candidate.Id, false, 0, "Unable to load item context.", null);

                await using var slot = await _ollamaJobGate.AcquireAvailableSlotsAsync(cancellationToken);
                return await ExecuteCandidateAsync(candidate, context, cancellationToken);
            });

            var results = await Task.WhenAll(tasks);

            var upserts = results
            .Where(r => r.Success && r.Result is not null)
            .Select(r => new AnalysedItemUpsert(
                r.CollectorItemId,
                r.Result!,
                JsonSerializer.Serialize(r.Result, JsonOptions),
                _ollamaOptions.Model,
                DateTime.UtcNow))
            .ToList();

            if (upserts.Count > 0)
                await _repository.UpsertAnalysedItemsBatchAsync(upserts, cancellationToken);

            foreach (var result in results)
            {
                evaluated++;
                if (result.Success)
                {
                    analysed++;
                    continue;
                }

                if (result.Attempts > 0)
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
            return new LLMAnalysisExecutionResult(collectorItemId, false, 0, "Collector item was not found.", null);

        if (candidate.CurrentStage != AnalysisStages.ReadyForAnalysis)
            return new LLMAnalysisExecutionResult(collectorItemId, false, 0,
                $"Collector item stage is {candidate.CurrentStage}; expected ReadyForAnalysis.", null);

        var context = await _repository.GetLlmAnalysisContextAsync(collectorItemId, cancellationToken);
        if (context is null)
            return new LLMAnalysisExecutionResult(collectorItemId, false, 0, "Unable to load item context.", null);

        return await ExecuteCandidateAsync(candidate, context, cancellationToken);
    }

    private async Task<LLMAnalysisExecutionResult> ExecuteCandidateAsync(LLMAnalysisCandidate candidate,LLMAnalysisContext context, CancellationToken cancellationToken)
    {
        var maxAttempts = Math.Max(1, _analysisOptions.MaxAttemptsPerItem);
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {

                var initialPrompt = LLMAnalysisPromptBuilder.BuildInitialPrompt(context);
                var estimatedTokens = (int)(initialPrompt.Length / 4.7);
                _logger.LogDebug("[Analysis] Estimated prompt tokens: {Tokens}", estimatedTokens);

                var modelOutput = await _ollamaHttpClient.GenerateAsync(initialPrompt,false, contextConfiguration.AnalysisContextSize, cancellationToken);

                if (string.IsNullOrWhiteSpace(modelOutput))
                {
                    continue;
                }

                var normalizedOutput = NormalizeModelOutput(modelOutput);

                if (TryParseResult(normalizedOutput, out var result, out var validationError))
                {
                    return new LLMAnalysisExecutionResult(candidate.Id, true, attempt, "Analysis succeeded.", result);
                }

                var repaired = await TryRepairResponseAsync(initialPrompt, validationError!, cancellationToken);
                if (repaired is not null)
                {
                    return new LLMAnalysisExecutionResult(candidate.Id, true, attempt, "Analysis succeeded after response repair.", repaired);
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

    private async Task<LLMAnalysisResult?> TryRepairResponseAsync(
        string badResponse,
        string error,
        CancellationToken cancellationToken)
    {
        var maxRepairAttempts = Math.Max(1, _analysisOptions.MaxRepairAttempts);
        var previousResponse = badResponse;

        for (var repairAttempt = 1; repairAttempt <= maxRepairAttempts; repairAttempt++)
        {
            var repairPrompt = LLMAnalysisPromptBuilder.BuildRepairPrompt(previousResponse, error);

            var estimatedRepairTokens = (int)(repairPrompt.Length / 4.7);
            _logger.LogDebug("[Analysis] Estimated prompt tokens: {Tokens}", estimatedRepairTokens);

            var repairedResponse = await _ollamaHttpClient.GenerateAsync(repairPrompt, false, contextConfiguration.AnalysisContextSize, cancellationToken);
            if (string.IsNullOrWhiteSpace(repairedResponse))
            {
                continue;
            }

            var normalized = NormalizeModelOutput(repairedResponse);
            if (TryParseResult(normalized, out var repairedResult, out _))
            {
                return repairedResult!;
            }

            previousResponse = normalized;
        }

        return null;
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

        result = NormalizeResult(result);

        if (string.IsNullOrWhiteSpace(result.ProblemSummary))
        {
            error = "ProblemSummary is required and must describe the post content.";
            return false;
        }

        if (result.ContainsProblem)
        {
            if (!result.SoftwareOpportunity)
            {
                error = "ContainsProblem cannot be true when SoftwareOpportunity is false.";
                return false;
            }

            if (!result.IsActionable)
            {
                error = "ContainsProblem cannot be true when IsActionable is false.";
                return false;
            }

            if (result.Actor is null)
            {
                error = "Actor is required when ContainsProblem is true.";
                return false;
            }

            if (result.ProblemDetails is null)
            {
                error = "ProblemDetails is required when ContainsProblem is true.";
                return false;
            }

            if (result.DesiredOutcome is null && result.CurrentWorkaround is null)
            {
                error = "At least one of DesiredOutcome or CurrentWorkaround is required when ContainsProblem is true.";
                return false;
            }

            if (result.ActionabilityRationale is null)
            {
                error = "ActionabilityRationale is required when ContainsProblem is true.";
                return false;
            }
        }
        else
        {
            if (result.SoftwareOpportunity)
            {
                error = "SoftwareOpportunity must be false when ContainsProblem is false.";
                return false;
            }

            if (result.IsActionable)
            {
                error = "IsActionable must be false when ContainsProblem is false.";
                return false;
            }

            if (result.Actor is not null ||
                result.ProblemDetails is not null ||
                result.CurrentWorkaround is not null ||
                result.DesiredOutcome is not null ||
                result.ActionabilityRationale is not null)
            {
                error = "Optional detail fields must be null when ContainsProblem is false.";
                return false;
            }
        }

        return true;
    }

    private static LLMAnalysisResult NormalizeResult(LLMAnalysisResult result)
    {
        // Coerce empty strings to null for all optional text fields.
        var normalized = result with
        {
            ProblemDetails = NullIfEmpty(result.ProblemDetails),
            Actor = NullIfEmpty(result.Actor),
            CurrentWorkaround = NullIfEmpty(result.CurrentWorkaround),
            DesiredOutcome = NullIfEmpty(result.DesiredOutcome),
            ActionabilityRationale = NullIfEmpty(result.ActionabilityRationale),
            UrgencySignal = result.UrgencySignal?.ToLowerInvariant() ?? "low",
        };

        return normalized;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

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
