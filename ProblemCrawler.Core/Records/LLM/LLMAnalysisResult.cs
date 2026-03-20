namespace ProblemCrawler.Core.Records.LLM;

public sealed record LLMAnalysisResult(
    bool ContainsProblem,
    string? ProblemSummary,
    string? ExpandedProblem,
    string Industry,
    string? Actor,
    string? CurrentSolution,
    int PainLevel,
    string FrequencySignal,
    bool SoftwareOpportunity,
    bool AutomationPotential,
    bool IsB2B,
    bool IsActionable,
    double Confidence);

public sealed record AnalysedItemUpsert(
    Guid CollectorItemId,
    LLMAnalysisResult Result,
    string RawJson,
    string Model,
    DateTime AnalyzedAtUtc);
