namespace ProblemCrawler.Core.Records.LLM;

public sealed record ThreadSynthesisRunSummary(
    int Evaluated,
    int Synthesized,
    int Skipped,
    int Failed);

public sealed record ThreadSynthesisExecutionResult(
    Guid RootCollectorItemId,
    bool Success,
    int Attempts,
    string? Message,
    int SynthesizedIdeaCount);