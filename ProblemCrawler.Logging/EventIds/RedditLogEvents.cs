using Microsoft.Extensions.Logging;

namespace ProblemCrawler.Logging.EventIds;

public static class RedditLogEvents
{
    public static readonly EventId CollectorStarted = new(1000, nameof(CollectorStarted));
    public static readonly EventId RequestUnexpectedError = new(1001, nameof(RequestUnexpectedError));
    public static readonly EventId RequestFailedAfterRetries = new(1002, nameof(RequestFailedAfterRetries));
}