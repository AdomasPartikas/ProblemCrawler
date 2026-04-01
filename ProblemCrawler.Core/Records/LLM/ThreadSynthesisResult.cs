namespace ProblemCrawler.Core.Records.LLM;

public sealed record ThreadSynthesisIdeaResult(
    string ProblemSummary,
    string? ProblemDetails,
    string? Actor,
    string Industry,
    string? CurrentWorkaround,
    string? DesiredOutcome,
    string UrgencySignal,
    bool SoftwareOpportunity,
    bool IsActionable,
    string? ActionabilityRationale,
    int SupportingMentionCount,
    int SupportingDistinctAuthorCount,
    string RawJson);

public sealed record ThreadSynthesisUpsert(
    Guid RootCollectorItemId,
    int ThreadItemCount,
    int AnalysedItemCount,
    DateTime LatestCollectorItemCreatedAtUtc,
    DateTime LatestAnalysedItemUpdatedAtUtc,
    IReadOnlyList<ThreadSynthesisIdeaResult> Ideas,
    string Model,
    DateTime AnalyzedAtUtc);