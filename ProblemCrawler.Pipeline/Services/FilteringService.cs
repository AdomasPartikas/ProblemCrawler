using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProblemCrawler.Core.Configuration;
using ProblemCrawler.Core.Enums;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Core.Records.Filtering;

namespace ProblemCrawler.Pipeline.Services;

/// <summary>
/// Applies filtering rules to collected content and marks each item with the proper stage.
/// </summary>
public sealed class FilteringService(
    ICollectorItemRepository repository,
    IOptions<FilteringConfiguration> filteringOptions,
    ILogger<FilteringService> logger) : IFilteringService
{
    private readonly ICollectorItemRepository _repository = repository;
    private readonly FilteringConfiguration _filteringOptions = filteringOptions.Value;
    private readonly ILogger<FilteringService> _logger = logger;

    private const string PostItemType = "Post";

    private static readonly HashSet<string> DeletedMarkers = FilteringWordLists.DeletedMarkers;

    public async Task<FilteringRunSummary> ExecuteAsync(CancellationToken cancellationToken)
    {
        var evaluated = 0;
        var updated = 0;
        var ready = 0;
        var removed = 0;
        var deleted = 0;

        var batchSize = _filteringOptions.BatchSize <= 0 ? 500 : _filteringOptions.BatchSize;

        while (!cancellationToken.IsCancellationRequested)
        {
            var candidates = await _repository.GetFilteringCandidatesAsync(batchSize, cancellationToken);
            if (candidates.Count == 0)
            {
                break;
            }

            var updates = new List<CollectorItemFilterUpdate>(candidates.Count);

            foreach (var candidate in candidates)
            {
                var targetStage = DetermineStage(candidate);
                evaluated++;

                if (targetStage == AnalysisStages.ReadyForAnalysis)
                {
                    ready++;
                }
                else if (targetStage == AnalysisStages.Removed)
                {
                    removed++;
                }
                else if (targetStage == AnalysisStages.Deleted)
                {
                    deleted++;
                }

                if (targetStage != candidate.CurrentStage)
                {
                    updates.Add(new CollectorItemFilterUpdate(candidate.Id, targetStage));
                }
            }

            if (updates.Count > 0)
            {
                await _repository.UpdateAnalysisStagesAsync(updates, cancellationToken);
                updated += updates.Count;
            }

            if (candidates.Count < batchSize)
            {
                break;
            }
        }

        var summary = new FilteringRunSummary(evaluated, ready, removed, deleted, updated);

        _logger.LogInformation(
            "Filtering completed. Evaluated: {Evaluated}, ready: {Ready}, removed: {Removed}, deleted: {Deleted}, updated: {Updated}",
            summary.Evaluated,
            summary.ReadyForAnalysis,
            summary.Removed,
            summary.Deleted,
            summary.Updated);

        return summary;
    }

    private static bool ContainsAlphaNumeric(string normalizedContent)
    {
        foreach (var ch in normalizedContent)
        {
            if (char.IsLetterOrDigit(ch))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeContent(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var normalizedWhitespace = string.Join(" ", content
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        return normalizedWhitespace.Trim();
    }

    private AnalysisStages DetermineStage(
        CollectorItemFilterCandidate candidate)
    {
        var normalized = NormalizeContent(candidate.Content);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return AnalysisStages.Removed;
        }

        if (IsDeletedMarker(normalized))
        {
            return AnalysisStages.Deleted;
        }

        var wordCountThreshold = candidate.ItemType == PostItemType
            ? _filteringOptions.MinimumWordCount
            : _filteringOptions.MinimumWordCount * 2;

        if (HasTooFewWords(normalized, wordCountThreshold) || HasTooFewMeaningfulCharacters(normalized) || !ContainsAlphaNumeric(normalized))
        {
            return AnalysisStages.Removed;
        }

        return AnalysisStages.ReadyForAnalysis;
    }

    private static bool IsDeletedMarker(string normalizedContent)
    {
        return DeletedMarkers.Contains(normalizedContent);
    }

    private static bool HasTooFewWords(string normalizedContent, int minimumWordCount)
    {
        var effectiveMinimum = Math.Max(1, minimumWordCount);
        var words = normalizedContent.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length < effectiveMinimum;
    }

    private bool HasTooFewMeaningfulCharacters(string normalizedContent)
    {
        var minimumCharacters = Math.Max(1, _filteringOptions.MinimumMeaningfulCharacters);
        var meaningfulCount = normalizedContent.Count(char.IsLetterOrDigit);
        return meaningfulCount < minimumCharacters;
    }
}
