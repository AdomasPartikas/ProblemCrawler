using Microsoft.Extensions.Logging;
using ProblemCrawler.Logging.EventIds;

namespace ProblemCrawler.Logging.Methods;

public static class RedditLogs
{
    public static void LogRedditCollectorStarted(this ILogger logger, int subredditCount) =>
        logger.LogInformation(RedditLogEvents.CollectorStarted, "Reddit collector started. Subreddits: {SubredditCount}", subredditCount);

    public static void LogRedditRequestUnexpectedError(this ILogger logger, Exception ex) =>
        logger.LogError(RedditLogEvents.RequestUnexpectedError, ex, "Unexpected error while fetching data from Reddit.");

    public static void LogRedditRequestFailedAfterRetries(this ILogger logger, int maxRetries) =>
        logger.LogError(RedditLogEvents.RequestFailedAfterRetries, "Reddit request failed after {MaxRetries} attempts.", maxRetries);
}