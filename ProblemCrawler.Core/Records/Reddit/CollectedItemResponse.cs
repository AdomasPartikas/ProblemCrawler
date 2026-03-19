namespace ProblemCrawler.Core.Records.Reddit
{
    public sealed record CollectedItemResponse(
        string Id,
        string ItemType,
        string? Author,
        DateTime CreatedAt,
        string? SourceUrl);
}
