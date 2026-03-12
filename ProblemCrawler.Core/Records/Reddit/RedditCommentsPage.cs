using ProblemCrawler.Core.Models.Reddit;

namespace ProblemCrawler.Core.Records.Reddit;

public sealed record RedditCommentsPage(string? After, IReadOnlyList<RedditComment> Comments)
{
    public static RedditCommentsPage Empty { get; } = new(null, []);
}
