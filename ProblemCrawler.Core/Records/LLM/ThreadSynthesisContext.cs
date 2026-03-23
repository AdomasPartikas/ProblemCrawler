namespace ProblemCrawler.Core.Records.LLM;

public sealed record ThreadSynthesisSourceItem(
    Guid AnalysedItemId,
    Guid CollectorItemId,
    string SourceId,
    string ItemType,
    string? Title,
    string? Content,
    string? ParentId,
    string? LinkId,
    string? Author,
    DateTime CreatedAtUtc,
    string? SourceUrl,
    bool ContainsProblem,
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
    DateTime AnalysedAtUtc,
    DateTime AnalysedUpdatedAtUtc);

public sealed record ThreadSynthesisContext(
    Guid RootCollectorItemId,
    LLMContextItem Root,
    IReadOnlyList<ThreadSynthesisSourceItem> Items,
    int ThreadItemCount,
    int AnalysedItemCount,
    DateTime LatestCollectorItemCreatedAtUtc,
    DateTime LatestAnalysedItemUpdatedAtUtc);