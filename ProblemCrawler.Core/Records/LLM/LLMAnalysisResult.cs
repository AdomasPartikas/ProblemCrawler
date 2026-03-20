namespace ProblemCrawler.Core.Records.LLM;

public sealed record LLMAnalysisResult(
    bool ContainsProblem,
    string? ProblemSummary,
    string? ProblemDetails,
    string? Actor,
    string Industry,
    string? CurrentWorkaround,
    string? DesiredOutcome,
    int PainLevel,
    string UrgencySignal,
    bool SoftwareOpportunity,
    bool IsActionable,
    string? ActionabilityRationale,
    double Confidence);

public sealed record AnalysedItemUpsert(
    Guid CollectorItemId,
    LLMAnalysisResult Result,
    string RawJson,
    string Model,
    DateTime AnalyzedAtUtc);
