namespace ProblemCrawler.Infrastructure.Entities;

public sealed class ThreadSynthesisRunEntity
{
    public Guid Id { get; set; }
    public Guid RootCollectorItemId { get; set; }
    public int ThreadItemCount { get; set; }
    public int AnalysedItemCount { get; set; }
    public DateTime LatestCollectorItemCreatedAtUtc { get; set; }
    public DateTime LatestAnalysedItemUpdatedAtUtc { get; set; }
    public string Model { get; set; } = string.Empty;
    public DateTime AnalyzedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<ThreadSynthesizedIdeaEntity> Ideas { get; set; } = [];
}