namespace ProblemCrawler.Infrastructure.Entities;

public sealed class ThreadSynthesizedIdeaEntity
{
    public Guid Id { get; set; }
    public Guid ThreadSynthesisRunId { get; set; }
    public string ProblemSummary { get; set; } = string.Empty;
    public string? ProblemDetails { get; set; }
    public string? Actor { get; set; }
    public string Industry { get; set; } = "unknown";
    public string? CurrentWorkaround { get; set; }
    public string? DesiredOutcome { get; set; }
    public string UrgencySignal { get; set; } = "low";
    public bool SoftwareOpportunity { get; set; }
    public bool IsActionable { get; set; }
    public string? ActionabilityRationale { get; set; }
    public int SupportingMentionCount { get; set; }
    public int SupportingDistinctAuthorCount { get; set; }
    public string RawJson { get; set; } = "{}";
    public DateTime AnalyzedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ThreadSynthesisRunEntity ThreadSynthesisRun { get; set; } = null!;
}