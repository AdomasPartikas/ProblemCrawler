using Microsoft.Extensions.Logging;
using ProblemCrawler.Logging.EventIdConstants;
using ProblemCrawler.Logging.EventIds;

namespace ProblemCrawler.Logging.LogMessages;

public static partial class CollectorHttpLogMessages
{
    [LoggerMessage(EventId = CollectorHttpEventIdConstants.RedditRequestSucceededId, Level = LogLevel.Debug, Message = "Reddit request succeeded. Url: {Url}, status: {StatusCode}, attempt: {Attempt}/{MaxRetries}")]
    public static partial void LogRedditRequestSucceeded(this ILogger logger, string url, int statusCode, int attempt, int maxRetries);

    [LoggerMessage(EventId = CollectorHttpEventIdConstants.RedditRequestRateLimitedId, Level = LogLevel.Warning, Message = "Reddit request rate-limited (429). Url: {Url}, retry after: {RetryAfterSeconds}s, attempt: {Attempt}/{MaxRetries}")]
    public static partial void LogRedditRequestRateLimited(this ILogger logger, string url, double retryAfterSeconds, int attempt, int maxRetries);

    [LoggerMessage(EventId = CollectorHttpEventIdConstants.RedditRequestRetryingId, Level = LogLevel.Debug, Message = "Retrying Reddit request. Url: {Url}, status: {StatusCode}, attempt: {Attempt}/{MaxRetries}, delay: {DelayMs}ms")]
    public static partial void LogRedditRequestRetrying(this ILogger logger, string url, int statusCode, int attempt, int maxRetries, int delayMs);

    [LoggerMessage(EventId = CollectorHttpEventIdConstants.RedditRequestUnexpectedErrorId, Level = LogLevel.Warning, Message = "Unexpected error while fetching Reddit data. Url: {Url}, attempt: {Attempt}/{MaxRetries}")]
    public static partial void LogRedditRequestUnexpectedError(this ILogger logger, Exception exception, string url, int attempt, int maxRetries);

    [LoggerMessage(EventId = CollectorHttpEventIdConstants.RedditRequestFailedAfterRetriesId, Level = LogLevel.Error, Message = "Reddit request failed after retries. Url: {Url}, max retries: {MaxRetries}")]
    public static partial void LogRedditRequestFailedAfterRetries(this ILogger logger, string url, int maxRetries);
}
