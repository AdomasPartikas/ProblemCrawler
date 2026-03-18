using ProblemCrawler.Core.Models.Reddit;

namespace ProblemCrawler.Core.Records.Reddit;

public sealed record RedditPostsPage(string? After, IReadOnlyList<RedditPost> Posts)
{
    public static RedditPostsPage Empty { get; } = new(null, []);
}
