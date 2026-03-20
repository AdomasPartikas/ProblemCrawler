namespace ProblemCrawler.Core.Records.LLM;

public sealed record LLMContextItem(
    string SourceId,
    string ItemType,
    string? Content,
    string? Author,
    DateTime CreatedAt,
    string? SourceUrl);

public sealed record LLMAnalysisContext(
    LLMContextItem Current,
    LLMContextItem? Parent,
    LLMContextItem? Post);
