namespace ProblemCrawler.Core.Constants;

/// <summary>
/// Core-owned list of subreddits targeted by the Reddit collector.
/// This keeps source selection separate from runtime collector behavior.
/// </summary>
public static class RedditSubredditCatalog
{
    public static IReadOnlyList<string> All { get; } =
    [
        "freelancing"
    ];
}