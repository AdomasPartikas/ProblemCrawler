using ProblemCrawler.Core.Models.Reddit;

namespace ProblemCrawler.Collectors.Reddit.Records;

public sealed record RedditPostsPage(string? After, IReadOnlyList<RedditPost> Posts)
{
    public static RedditPostsPage Empty { get; } = new(null, []);
}
