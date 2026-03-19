using ProblemCrawler.Core.Enums;

namespace ProblemCrawler.Core.Records.Filtering;

public sealed record CollectorItemFilterCandidate(
    Guid Id,
    string? Content,
    AnalysisStages CurrentStage);
