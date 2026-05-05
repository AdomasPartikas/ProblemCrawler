using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProblemCrawler.Core.Configuration;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Core.Records.LLM;
using ProblemCrawler.Pipeline.Clients;
using ProblemCrawler.Pipeline.Helper;
using ProblemCrawler.Pipeline.Prompts;
using System.Text.Json;

namespace ProblemCrawler.Pipeline.Services;

public sealed class ThreadSynthesisService(
    ICollectorItemRepository repository,
    OllamaHttpClient ollamaHttpClient,
    OllamaJobGate ollamaJobGate,
    IOptions<ThreadSynthesisConfiguration> synthesisOptions,
    IOptions<OllamaConfiguration> ollamaOptions,
    ILogger<ThreadSynthesisService> logger) : IThreadSynthesisService
{
    private readonly ICollectorItemRepository _repository = repository;
    private readonly OllamaHttpClient _ollamaHttpClient = ollamaHttpClient;
    private readonly ThreadSynthesisConfiguration _synthesisOptions = synthesisOptions.Value;
    private readonly OllamaConfiguration _ollamaOptions = ollamaOptions.Value;
    private readonly ILogger<ThreadSynthesisService> _logger = logger;
    private readonly OllamaJobGate _ollamaJobGate = ollamaJobGate;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> AllowedUrgencySignals = ["low", "medium", "high"];

    public async Task<ThreadSynthesisRunSummary> ExecuteAsync(CancellationToken cancellationToken)
    {
        await using var _ = await _ollamaJobGate.AcquireAsync(cancellationToken);
        var evaluated = 0;
        var synthesized = 0;
        var skipped = 0;
        var failed = 0;

        var batchSize = _synthesisOptions.BatchSize <= 0 ? 25 : _synthesisOptions.BatchSize;

        while (!cancellationToken.IsCancellationRequested)
        {
            var candidates = await _repository.GetThreadSynthesisCandidatesAsync(batchSize, cancellationToken);
            if (candidates.Count == 0)
            {
                break;
            }

            foreach (var candidate in candidates)
            {
                evaluated++;
                var executionResult = await ExecuteForThreadAsync(candidate.RootCollectorItemId, cancellationToken);
                if (executionResult.Success)
                {
                    synthesized++;
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

        var summary = new ThreadSynthesisRunSummary(evaluated, synthesized, skipped, failed);

        _logger.LogInformation(
            "Thread synthesis completed. Evaluated: {Evaluated}, synthesized: {Synthesized}, skipped: {Skipped}, failed: {Failed}",
            summary.Evaluated,
            summary.Synthesized,
            summary.Skipped,
            summary.Failed);

        return summary;
    }

    public async Task<ThreadSynthesisExecutionResult> ExecuteForThreadAsync(Guid rootCollectorItemId, CancellationToken cancellationToken)
    {
        var maxAttempts = Math.Max(1, _synthesisOptions.MaxAttemptsPerThread);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var context = await _repository.GetThreadSynthesisContextAsync(rootCollectorItemId, cancellationToken);
                if (context is null)
                {
                    await _repository.ReleaseSynthesisClaimAsync(rootCollectorItemId, cancellationToken);
                    return new ThreadSynthesisExecutionResult(
                        rootCollectorItemId,
                        false,
                        0,
                        "Unable to load thread synthesis context.",
                        0);
                }

                var initialPrompt = LLMAnalysisPromptBuilder.BuildThreadSynthesisPrompt(context);

                var estimatedTokens = (int)(initialPrompt.Length / 3.5);
                _logger.LogInformation("[synthesis] Estimated prompt tokens: {Tokens}", estimatedTokens);

                var modelOutput = await _ollamaHttpClient.GenerateAsync(initialPrompt, cancellationToken);
                if (string.IsNullOrWhiteSpace(modelOutput))
                {
                    _logger.LogWarning("[synthesis] Attempt {Attempt} returned empty response for {RootId}",
                    attempt, rootCollectorItemId);
                    continue;
                }

                var normalizedOutput = NormalizeModelOutput(modelOutput);

                if (TryParseResult(normalizedOutput, context.Items, out var result, out var validationError))
                {
                    await PersistResultAsync(context, result!, cancellationToken);
                    return new ThreadSynthesisExecutionResult(
                        rootCollectorItemId,
                        true,
                        attempt,
                        "Thread synthesis succeeded.",
                        result!.Count);
                }
                _logger.LogWarning("[synthesis] Parse failed for {RootId} — {Error}", rootCollectorItemId, validationError);
                var repaired = await TryRepairResponseAsync(
                    initialPrompt,
                    normalizedOutput,
                    validationError!,
                    context.Items,
                    cancellationToken);

                if (repaired is not null)
                {
                    await PersistResultAsync(context, repaired.Value.Result, cancellationToken);
                    return new ThreadSynthesisExecutionResult(
                        rootCollectorItemId,
                        true,
                        attempt,
                        "Thread synthesis succeeded after response repair.",
                        repaired.Value.Result.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Thread synthesis attempt {Attempt} failed for root item {RootCollectorItemId}", attempt, rootCollectorItemId);
            }
        }

        await _repository.ReleaseSynthesisClaimAsync(rootCollectorItemId, cancellationToken);

        return new ThreadSynthesisExecutionResult(
            rootCollectorItemId,
            false,
            maxAttempts,
            "All attempts exhausted for this thread; synthesis remains stale.",
            0);
    }

    private async Task<(IReadOnlyList<ThreadSynthesisIdeaResult> Result, string RawJson)?> TryRepairResponseAsync(
        string originalPrompt,
        string badResponse,
        string error,
        IReadOnlyList<ThreadSynthesisSourceItem> sourceItems,
        CancellationToken cancellationToken)
    {
        var maxRepairAttempts = Math.Max(1, _synthesisOptions.MaxRepairAttempts);
        var previousResponse = badResponse;

        for (var repairAttempt = 1; repairAttempt <= maxRepairAttempts; repairAttempt++)
        {
            _logger.LogWarning(
                "[synthesis] Repair attempt {RepairAttempt}/{Max} — reason: {Error}",
                repairAttempt, maxRepairAttempts, error);
            var repairPrompt = LLMAnalysisPromptBuilder.BuildRepairPrompt(originalPrompt, previousResponse, error);
            var estimatedRepairTokens = (int)(repairPrompt.Length / 3.5);
            _logger.LogDebug("[synthesis] Estimated repair prompt tokens: {Tokens}", estimatedRepairTokens);
            var repairedResponse = await _ollamaHttpClient.GenerateAsync(repairPrompt, cancellationToken);
            if (string.IsNullOrWhiteSpace(repairedResponse))
            {
                continue;
            }

            var normalized = NormalizeModelOutput(repairedResponse);
            if (TryParseResult(normalized, sourceItems, out var repairedResult, out _))
            {
                return (repairedResult!, normalized);
            }

            previousResponse = normalized;
        }

        return null;
    }

    private async Task PersistResultAsync(
        ThreadSynthesisContext context,
        IReadOnlyList<ThreadSynthesisIdeaResult> ideas,
        CancellationToken cancellationToken)
    {
        var analyzedAtUtc = DateTime.UtcNow;
        var upsert = new ThreadSynthesisUpsert(
            context.RootCollectorItemId,
            context.ThreadItemCount,
            context.AnalysedItemCount,
            context.LatestCollectorItemCreatedAtUtc,
            context.LatestAnalysedItemUpdatedAtUtc,
            ideas,
            _ollamaOptions.Model,
            analyzedAtUtc);

        await _repository.UpsertThreadSynthesisAsync(upsert, cancellationToken);
    }

    private static bool TryParseResult(
        string rawResponse,
        IReadOnlyList<ThreadSynthesisSourceItem> sourceItems,
        out IReadOnlyList<ThreadSynthesisIdeaResult>? result,
        out string? error)
    {
        result = null;
        error = null;

        ThreadSynthesisResponse? response;

        try
        {
            response = JsonSerializer.Deserialize<ThreadSynthesisResponse>(rawResponse, JsonOptions);
        }
        catch (Exception ex)
        {
            error = $"JSON deserialization failed: {ex.Message}";
            return false;
        }

        if (response?.Ideas is null)
        {
            error = "Response payload must contain an ideas array.";
            return false;
        }

        var normalizedIdeas = new List<ThreadSynthesisIdeaCandidate>(response.Ideas.Count);
        var sourceItemsByEvidenceNumber = sourceItems
            .Select((item, index) => new { EvidenceNumber = index + 1, Item = item })
            .ToDictionary(x => x.EvidenceNumber, x => x.Item);

        foreach (var rawIdea in response.Ideas)
        {
            if (rawIdea is null)
            {
                error = "Ideas array cannot contain null items.";
                return false;
            }

            var normalizedIdea = NormalizeIdea(rawIdea);

            if (string.IsNullOrWhiteSpace(normalizedIdea.ProblemSummary))
            {
                error = "ProblemSummary is required for every synthesized idea.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(normalizedIdea.Industry))
            {
                error = "Industry is required for every synthesized idea.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(normalizedIdea.Actor))
            {
                error = "Actor is required for every synthesized idea.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(normalizedIdea.ProblemDetails))
            {
                error = "ProblemDetails is required for every synthesized idea.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(normalizedIdea.UrgencySignal) || !AllowedUrgencySignals.Contains(normalizedIdea.UrgencySignal))
            {
                error = "UrgencySignal must be one of: low, medium, high.";
                return false;
            }

            if (!normalizedIdea.SoftwareOpportunity)
            {
                error = "softwareOpportunity must be true for synthesized ideas.";
                return false;
            }

            if (!normalizedIdea.IsActionable)
            {
                error = "isActionable must be true for synthesized ideas.";
                return false;
            }

            if (normalizedIdea.CurrentWorkaround is null && normalizedIdea.DesiredOutcome is null)
            {
                error = "At least one of DesiredOutcome or CurrentWorkaround is required for every synthesized idea.";
                return false;
            }

            if (normalizedIdea.ActionabilityRationale is null)
            {
                error = "ActionabilityRationale is required for every synthesized idea.";
                return false;
            }

            if (normalizedIdea.SupportingEvidenceNumbers is null || normalizedIdea.SupportingEvidenceNumbers.Count == 0)
            {
                error = "SupportingEvidenceNumbers must contain at least one evidence number.";
                return false;
            }

            var uniqueEvidenceNumbers = normalizedIdea.SupportingEvidenceNumbers
                .Distinct()
                .OrderBy(x => x)
                .ToArray();

            if (uniqueEvidenceNumbers.Length != normalizedIdea.SupportingEvidenceNumbers.Count)
            {
                error = "SupportingEvidenceNumbers must not contain duplicates.";
                return false;
            }

            if (uniqueEvidenceNumbers.Any(number => !sourceItemsByEvidenceNumber.ContainsKey(number)))
            {
                error = "SupportingEvidenceNumbers must reference only valid evidence items from the prompt.";
                return false;
            }

            normalizedIdeas.Add(new ThreadSynthesisIdeaCandidate(
                normalizedIdea.ProblemSummary,
                normalizedIdea.ProblemDetails,
                normalizedIdea.Actor!,
                normalizedIdea.Industry,
                normalizedIdea.CurrentWorkaround,
                normalizedIdea.DesiredOutcome,
                normalizedIdea.UrgencySignal,
                normalizedIdea.SoftwareOpportunity,
                normalizedIdea.IsActionable,
                normalizedIdea.ActionabilityRationale,
                uniqueEvidenceNumbers));
        }

        result = normalizedIdeas
            .GroupBy(CreateFingerprint)
            .Select(group => MergeCandidateGroup(group, sourceItemsByEvidenceNumber))
            .ToList();

        return true;
    }

    private static ThreadSynthesisIdea NormalizeIdea(ThreadSynthesisIdea idea)
    {
        return idea with
        {
            ProblemSummary = (idea.ProblemSummary ?? string.Empty).Trim(),
            ProblemDetails = NullIfEmpty(idea.ProblemDetails),
            Actor = NullIfEmpty(idea.Actor),
            Industry = (idea.Industry ?? string.Empty).Trim(),
            CurrentWorkaround = NullIfEmpty(idea.CurrentWorkaround),
            DesiredOutcome = NullIfEmpty(idea.DesiredOutcome),
            UrgencySignal = (idea.UrgencySignal ?? "low").Trim().ToLowerInvariant(),
            ActionabilityRationale = NullIfEmpty(idea.ActionabilityRationale),
            SupportingEvidenceNumbers = idea.SupportingEvidenceNumbers ?? []
        };
    }

    private static string CreateFingerprint(ThreadSynthesisIdeaCandidate idea)
    {
        return string.Join(
            '|',
            NormalizeFingerprintPart(idea.ProblemSummary),
            NormalizeFingerprintPart(idea.Actor),
            NormalizeFingerprintPart(idea.Industry));
    }

    private static string NormalizeFingerprintPart(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var chars = value
            .Trim()
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray();

        return chars.Length == 0 ? string.Empty : new string(chars);
    }

    private static ThreadSynthesisIdeaResult MergeCandidateGroup(
        IGrouping<string, ThreadSynthesisIdeaCandidate> group,
        IReadOnlyDictionary<int, ThreadSynthesisSourceItem> sourceItemsByEvidenceNumber)
    {
        var representative = group.First();
        var mergedEvidenceNumbers = group
            .SelectMany(candidate => candidate.SupportingEvidenceNumbers)
            .Distinct()
            .OrderBy(number => number)
            .ToArray();

        var supportingItems = mergedEvidenceNumbers
            .Select(number => sourceItemsByEvidenceNumber[number])
            .ToArray();

        var supportingMentionCount = supportingItems.Length;
        var supportingDistinctAuthorCount = supportingItems
            .Select(item => item.Author)
            .Where(author => !string.IsNullOrWhiteSpace(author))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        if (supportingDistinctAuthorCount == 0)
        {
            supportingDistinctAuthorCount = 1;
        }

        var rawJson = JsonSerializer.Serialize(new
        {
            representative.ProblemSummary,
            representative.ProblemDetails,
            representative.Actor,
            representative.Industry,
            representative.CurrentWorkaround,
            representative.DesiredOutcome,
            representative.UrgencySignal,
            representative.SoftwareOpportunity,
            representative.IsActionable,
            representative.ActionabilityRationale,
            SupportingEvidenceNumbers = mergedEvidenceNumbers
        }, JsonOptions);

        return new ThreadSynthesisIdeaResult(
            representative.ProblemSummary,
            representative.ProblemDetails,
            representative.Actor,
            representative.Industry,
            representative.CurrentWorkaround,
            representative.DesiredOutcome,
            representative.UrgencySignal,
            representative.SoftwareOpportunity,
            representative.IsActionable,
            representative.ActionabilityRationale,
            supportingMentionCount,
            supportingDistinctAuthorCount,
            rawJson);
    }

    private static string? NullIfEmpty(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        // Treat literal "null", "n/a", "none" as null (common placeholder strings from models)
        return trimmed.Equals("null", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Equals("n/a", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Equals("none", StringComparison.OrdinalIgnoreCase)
            ? null
            : trimmed;
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

    private sealed record ThreadSynthesisResponse(IReadOnlyList<ThreadSynthesisIdea>? Ideas);

    private sealed record ThreadSynthesisIdea(
        string? ProblemSummary,
        string? ProblemDetails,
        string? Actor,
        string? Industry,
        string? CurrentWorkaround,
        string? DesiredOutcome,
        string UrgencySignal,
        bool SoftwareOpportunity,
        bool IsActionable,
        string? ActionabilityRationale,
        IReadOnlyList<int>? SupportingEvidenceNumbers);

    private sealed record ThreadSynthesisIdeaCandidate(
        string ProblemSummary,
        string? ProblemDetails,
        string Actor,
        string Industry,
        string? CurrentWorkaround,
        string? DesiredOutcome,
        string UrgencySignal,
        bool SoftwareOpportunity,
        bool IsActionable,
        string? ActionabilityRationale,
        IReadOnlyList<int> SupportingEvidenceNumbers);
}