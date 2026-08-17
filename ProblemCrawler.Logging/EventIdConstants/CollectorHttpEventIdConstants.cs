namespace ProblemCrawler.Logging.EventIdConstants;

public static class CollectorHttpEventIdConstants
{
    // HTTP request events (200-229)
    public const int RedditRequestSucceededId = 200;
    public const int RedditRequestRateLimitedId = 201;
    public const int RedditRequestRetryingId = 202;
    public const int RedditRequestUnexpectedErrorId = 203;
    public const int RedditRequestFailedAfterRetriesId = 204;
}