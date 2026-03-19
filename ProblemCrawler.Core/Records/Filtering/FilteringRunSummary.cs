namespace ProblemCrawler.Core.Records.Filtering;

public sealed record FilteringRunSummary(
    int Evaluated,
    int ReadyForAnalysis,
    int Removed,
    int Deleted,
    int Updated);
