using ProblemCrawler.Core.Enums;

namespace ProblemCrawler.Core.Records.Filtering;

public sealed record CollectorItemFilterCandidate(
    Guid Id,
    string? Content,
    string? SelfText,
    AnalysisStages CurrentStage);
