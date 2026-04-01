namespace ProblemCrawler.Core.Records.LLM;

public sealed record ThreadSynthesisCandidate(
    Guid RootCollectorItemId,
    int ThreadItemCount,
    int AnalysedItemCount,
    DateTime LatestCollectorItemCreatedAtUtc,
    DateTime LatestAnalysedItemUpdatedAtUtc);