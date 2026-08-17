namespace ProblemCrawler.Logging.EventIdConstants;

public static class CollectorsEventIdConstants
{
    // Collector lifecycle events (100-109)
    public const int NoSubredditsConfiguredId = 100;
    public const int CollectorRunStartedId = 101;
    public const int CollectorRunCompletedId = 102;

    // Subreddit collection events (110-129)
    public const int SubredditCollectionStartedId = 110;
    public const int SubredditCollectionCompletedId = 111;
    public const int SubredditNoPostsFoundId = 112;
    public const int CollectorPageLimitReachedId = 113;
    public const int CommentLimitReachedId = 114;
}