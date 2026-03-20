using ProblemCrawler.Core.Enums;

namespace ProblemCrawler.Core.Records.LLM;

public sealed record LLMAnalysisCandidate(
    Guid Id,
    string Source,
    string SourceId,
    string ItemType,
    string? Content,
    string? ParentId,
    string? LinkId,
    AnalysisStages CurrentStage);
