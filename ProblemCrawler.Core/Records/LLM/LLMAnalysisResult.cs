namespace ProblemCrawler.Core.Records.LLM;

public sealed record LLMAnalysisResult(
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
    string? ActionabilityRationale);

public sealed record AnalysedItemUpsert(
    Guid CollectorItemId,
    LLMAnalysisResult Result,
    string RawJson,
    string Model,
    DateTime AnalyzedAtUtc);
