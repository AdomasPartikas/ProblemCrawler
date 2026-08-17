using Microsoft.Extensions.Logging;
using ProblemCrawler.Logging.EventIdConstants;

namespace ProblemCrawler.Logging.EventIds;

public static class CollectorHttpEventIds
{
    public static readonly EventId RedditRequestSucceeded = new(CollectorHttpEventIdConstants.RedditRequestSucceededId, nameof(RedditRequestSucceeded));
    public static readonly EventId RedditRequestRateLimited = new(CollectorHttpEventIdConstants.RedditRequestRateLimitedId, nameof(RedditRequestRateLimited));
    public static readonly EventId RedditRequestRetrying = new(CollectorHttpEventIdConstants.RedditRequestRetryingId, nameof(RedditRequestRetrying));
    public static readonly EventId RedditRequestUnexpectedError = new(CollectorHttpEventIdConstants.RedditRequestUnexpectedErrorId, nameof(RedditRequestUnexpectedError));
    public static readonly EventId RedditRequestFailedAfterRetries = new(CollectorHttpEventIdConstants.RedditRequestFailedAfterRetriesId, nameof(RedditRequestFailedAfterRetries));
}
