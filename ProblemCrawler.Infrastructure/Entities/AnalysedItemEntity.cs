namespace ProblemCrawler.Infrastructure.Entities;

public sealed class AnalysedItemEntity
{
    public Guid Id { get; set; }
    public Guid CollectorItemId { get; set; }

    public bool ContainsProblem { get; set; }
    public string ProblemSummary { get; set; } = string.Empty;
    public string? ProblemDetails { get; set; }
    public string? Actor { get; set; }
    public string Industry { get; set; } = "unknown";
    public string? CurrentWorkaround { get; set; }
    public string? DesiredOutcome { get; set; }
    public int? PainLevel { get; set; }
    public string UrgencySignal { get; set; } = "low";
    public bool SoftwareOpportunity { get; set; }
    public bool IsActionable { get; set; }
    public string? ActionabilityRationale { get; set; }
    public decimal? Confidence { get; set; }

    public string RawJson { get; set; } = "{}";
    public string Model { get; set; } = string.Empty;
    public DateTime AnalyzedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public CollectorItemEntity CollectorItem { get; set; } = null!;
}
