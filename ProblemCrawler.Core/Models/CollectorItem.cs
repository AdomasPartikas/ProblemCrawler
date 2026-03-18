using ProblemCrawler.Core.Interfaces;

namespace ProblemCrawler.Core.Models;

/// <summary>
/// Normalized collector item shape used across all data sources.
/// </summary>
public class CollectorItem : ICollectorItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string SourceId { get; set; }
    public required string ItemType { get; set; }
    public required string Source { get; set; }
    public string? Content { get; set; }
    public Dictionary<string, object?> Metadata { get; set; } = [];
    public required DateTime CreatedAt { get; set; }
    public string? Author { get; set; }
    public string? SourceUrl { get; set; }
}