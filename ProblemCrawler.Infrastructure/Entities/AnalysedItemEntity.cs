namespace ProblemCrawler.Infrastructure.Entities;

public sealed class AnalysedItemEntity
{
    public Guid Id { get; set; }
    public Guid CollectorItemId { get; set; }

    public bool ContainsProblem { get; set; }
    public string? ProblemSummary { get; set; }
    public string? ExpandedProblem { get; set; }
    public string Industry { get; set; } = "unknown";
    public string? Actor { get; set; }
    public string? CurrentSolution { get; set; }
    public int PainLevel { get; set; }
    public string FrequencySignal { get; set; } = "low";
    public bool SoftwareOpportunity { get; set; }
    public bool AutomationPotential { get; set; }
    public bool IsB2B { get; set; }
    public bool IsActionable { get; set; }
    public decimal Confidence { get; set; }

    public string RawJson { get; set; } = "{}";
    public string Model { get; set; } = string.Empty;
    public DateTime AnalyzedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public CollectorItemEntity CollectorItem { get; set; } = null!;
}
