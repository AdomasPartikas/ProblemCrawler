using ProblemCrawler.Core.Enums;

namespace ProblemCrawler.Core.Records.Filtering;

public sealed record CollectorItemFilterUpdate(Guid Id, AnalysisStages TargetStage);
