using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProblemCrawler.Core.Configuration;
using ProblemCrawler.Core.Factory;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Core.Records.LLM;
using ProblemCrawler.Pipeline.Clients;
using ProblemCrawler.Pipeline.Helper;
using ProblemCrawler.Pipeline.Prompts;
using System.Text.Json;

namespace ProblemCrawler.Pipeline.Services;

public sealed class ThreadSynthesisService(
    RepositoryScope repositoryScope,
    OllamaHttpClient ollamaHttpClient,
    OllamaJobGate ollamaJobGate,
    IOptions<ThreadSynthesisConfiguration> synthesisOptions,
    OllamaContextConfiguration contextConfiguration,
    IOptions<OllamaConfiguration> ollamaOptions,
    ILogger<ThreadSynthesisService> logger) : IThreadSynthesisService
{
    private readonly RepositoryScope _repositoryScope = repositoryScope;
    private readonly OllamaHttpClient _ollamaHttpClient = ollamaHttpClient;
    private readonly ThreadSynthesisConfiguration _synthesisOptions = synthesisOptions.Value;
    private readonly OllamaConfiguration _ollamaOptions = ollamaOptions.Value;
    private readonly ILogger<ThreadSynthesisService> _logger = logger;
    private readonly OllamaJobGate _ollamaJobGate = ollamaJobGate;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> AllowedUrgencySignals = ["low", "medium", "high"];

    public async Task<ThreadSynthesisRunSummary> ExecuteAsync(CancellationToken cancellationToken)
    {
        await using var _ = await _ollamaJobGate.AcquireJobAsync(cancellationToken);
        _ollamaJobGate.StartDiagnosticLogging(_logger, cancellationToken);
        var evaluated = 0;
        var synthesized = 0;
        var skipped = 0;
        var failed = 0;

        var batchSize = _synthesisOptions.BatchSize <= 0 ? 25 : _synthesisOptions.BatchSize;
        var (loaderRepo, loaderScope) = await _repositoryScope.CreateAsync();
        await using (loaderScope)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var candidates = await loaderRepo.GetThreadSynthesisCandidatesAsync(batchSize, cancellationToken);
                if (candidates.Count == 0)
                {
                    break;
                }
                var contexts = new Dictionary<Guid, ThreadSynthesisContext>();
                foreach (var candidate in candidates)
                {
                    var ctx = await loaderRepo.GetThreadSynthesisContextAsync(candidate.RootCollectorItemId, cancellationToken);
                    if (ctx is not null) contexts[candidate.RootCollectorItemId] = ctx;
                    else await loaderRepo.ReleaseSynthesisClaimAsync(candidate.RootCollectorItemId, cancellationToken);
                }
                var tasks = candidates.Select(async candidate =>
                {
                    if (!contexts.TryGetValue(candidate.RootCollectorItemId, out var context))
                        return new ThreadSynthesisExecutionResult(candidate.RootCollectorItemId, false, 0, "Unable to load thread synthesis context.", 0);

                    var (repo, scope) = await _repositoryScope.CreateAsync();
                    await using (scope)
                    {
                        return await ExecuteForThreadAsync(candidate.RootCollectorItemId, context, repo, cancellationToken);
                    }
                });
                var results = await Task.WhenAll(tasks);

                foreach (var result in results)
                {
                    evaluated++;
                    if (result.Success) { synthesized++; continue; }
                    if (result.Attempts > 0) failed++;
                    else skipped++;
                }

                if (candidates.Count < batchSize) break;
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

    public async Task<ThreadSynthesisExecutionResult> ExecuteForThreadAsync(Guid rootCollectorItemId, ThreadSynthesisContext context, ICollectorItemRepository repository, CancellationToken cancellationToken)
    {
        var maxAttempts = Math.Max(1, _synthesisOptions.MaxAttemptsPerThread);
        var initialPrompt = LLMAnalysisPromptBuilder.BuildThreadSynthesisPrompt(context);
        var estimatedTokens = (int)(initialPrompt.Length / 3.5);
        var isLarge = estimatedTokens > contextConfiguration.SynthesisContextSize;
        var contextSize = isLarge ? contextConfiguration.FullContextSize : contextConfiguration.SynthesisContextSize;
        var tokK = estimatedTokens >= 1000 ? $"{estimatedTokens / 1000.0:F1}k" : estimatedTokens.ToString();
        var ctxK = contextSize / 1024;

        IReadOnlyList<ThreadSynthesisIdeaResult>? parsedResult = null;
        var successAttempt = 0;
        var repaired = false;

        await using (var slot = isLarge
            ? await _ollamaJobGate.AcquireFullContextSlotAsync(cancellationToken)
            : await _ollamaJobGate.AcquireSynthesisSlotAsync(cancellationToken))
        {

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    var modelOutput = await _ollamaHttpClient.GenerateAsync(initialPrompt, false, contextSize, cancellationToken);

                    if (string.IsNullOrWhiteSpace(modelOutput))
                    {
                        _logger.LogWarning("[synthesis] ~{Tok}tok  ctx={Ctx}k attempt={Attempt} -> empty",
                            tokK, ctxK, attempt);
                        continue;
                    }

                    var normalizedOutput = NormalizeModelOutput(modelOutput);

                    if (TryParseResult(normalizedOutput, context.Items, out var result, out var validationError))
                    {
                        _logger.LogInformation("[synthesis] ~{Tok}tok  ctx={Ctx}k attempt={Attempt} -> {Count} idea(s)",
                            tokK, ctxK, attempt, result!.Count);
                        parsedResult = result;
                        successAttempt = attempt;
                        break;
                    }

                    _logger.LogWarning("[synthesis] ~{Tok}tok  ctx={Ctx}k attempt={Attempt} -> parse fail: {Error}",
                         tokK, ctxK, attempt, validationError);

                    var repairedResult = await TryRepairResponseAsync(
                        initialPrompt, normalizedOutput, validationError!,
                        contextSize, context.Items, cancellationToken);

                    if (repairedResult is not null)
                    {
                        _logger.LogInformation("[synthesis] ~{Tok}tok  ctx={Ctx}k  attempt={Attempt} → repaired  {Count} idea(s)",
                            tokK, ctxK, attempt, repairedResult.Value.Result.Count);
                        parsedResult = repairedResult.Value.Result;
                        successAttempt = attempt;
                        repaired = true;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[synthesis] attempt {Attempt} threw for {RootId}", attempt, rootCollectorItemId);
                }
            }
        }

        if (parsedResult is not null)
        {
            await PersistResultAsync(context, parsedResult, repository, cancellationToken);
            return new ThreadSynthesisExecutionResult(
                rootCollectorItemId, true, successAttempt,
                repaired ? "Thread synthesis succeeded after response repair." : "Thread synthesis succeeded.",
                parsedResult.Count);
        }

        await repository.ReleaseSynthesisClaimAsync(rootCollectorItemId, cancellationToken);

        _logger.LogError("[synthesis] ~{Tok}tok  ctx={Ctx}k exhausted after {Max} attempts  root={RootId}",
         tokK, ctxK, maxAttempts, rootCollectorItemId);

        return new ThreadSynthesisExecutionResult(
            rootCollectorItemId, false, maxAttempts,
            "All attempts exhausted for this thread; synthesis remains stale.", 0);
    }

    private async Task<(IReadOnlyList<ThreadSynthesisIdeaResult> Result, string RawJson)?> TryRepairResponseAsync(
        string originalPrompt,
        string badResponse,
        string error,
        int contextSize,
        IReadOnlyList<ThreadSynthesisSourceItem> sourceItems,
        CancellationToken cancellationToken)
    {
        var maxRepairAttempts = Math.Max(1, _synthesisOptions.MaxRepairAttempts);
        var previousResponse = badResponse;
        var ctxK = contextSize / 1024;

        for (var repairAttempt = 1; repairAttempt <= maxRepairAttempts; repairAttempt++)
        {
            var repairPrompt = LLMAnalysisPromptBuilder.BuildRepairPrompt(previousResponse, error);
            var estimatedTok = (int)(repairPrompt.Length / 4.7);
            var tokK = estimatedTok >= 1000 ? $"{estimatedTok / 1000.0:F1}k" : estimatedTok.ToString();

            var repairedResponse = await _ollamaHttpClient.GenerateAsync(repairPrompt, false, contextSize, cancellationToken);

            if (string.IsNullOrWhiteSpace(repairedResponse))
            {
                _logger.LogWarning("[repair] {Attempt}/{Max}  ~{Tok}tok  ctx={Ctx}k, empty",
                    repairAttempt, maxRepairAttempts, tokK, ctxK);
                continue;
            }

            var normalized = NormalizeModelOutput(repairedResponse);

            if (TryParseResult(normalized, sourceItems, out var repairedResult, out var newError))
            {
                _logger.LogInformation("[repair] {Attempt}/{Max}  ~{Tok}tok  ctx={Ctx}k, {Count} idea(s)",
                    repairAttempt, maxRepairAttempts, tokK, ctxK, repairedResult!.Count);
                return (repairedResult!, normalized);
            }

            _logger.LogWarning("[repair] {Attempt}/{Max}  ~{Tok}tok  ctx={Ctx}k, still broken: {Error}",
                repairAttempt, maxRepairAttempts, tokK, ctxK, newError);

            error = newError!;
            previousResponse = normalized;
        }

        _logger.LogWarning("[repair] gave up after {Max} attempts — last error: {Error}", maxRepairAttempts, error);
        return null;
    }

    private async Task PersistResultAsync(
        ThreadSynthesisContext context,
        IReadOnlyList<ThreadSynthesisIdeaResult> ideas,
        ICollectorItemRepository repository,
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

        await repository.UpsertThreadSynthesisAsync(upsert, cancellationToken);
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