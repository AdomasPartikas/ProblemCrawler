namespace ProblemCrawler.Core.Records.LLM;

public sealed record LLMAnalysisRunSummary(
    int Evaluated,
    int Analysed,
    int Skipped,
    int Failed);

public sealed record LLMAnalysisExecutionResult(
    Guid CollectorItemId,
    bool Success,
    int Attempts,
    string? Message,
    LLMAnalysisResult? Result);
