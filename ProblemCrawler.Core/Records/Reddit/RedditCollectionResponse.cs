namespace ProblemCrawler.Core.Records.Reddit
{
    public sealed record RedditCollectionResponse(
    string Message,
    int TotalItems,
    IReadOnlyList<CollectedItemResponse> Items);
}
