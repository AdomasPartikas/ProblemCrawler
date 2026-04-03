using Microsoft.Extensions.Logging;

namespace ProblemCrawler.Logging.LoggerMessages;

public static partial class CollectorLoggerMessages
{
    // Informational lifecycle events.
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Collector started. Collector: {Collector}, Subreddits: {SubredditCount}")]
    public static partial void LogCollectorStarted(this ILogger logger, string collector, int subredditCount);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Subreddit collection started. Collector: {Collector}, Subreddit: {Subreddit}, MaxPages: {MaxPages}, FetchComments: {FetchComments}, RequestDelayMs: {RequestDelayMs}")]
    public static partial void LogCollectorSubredditCollectionStarted(this ILogger logger, string collector, string subreddit, int? maxPages, bool fetchComments, int requestDelayMs);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Subreddit page fetched. Collector: {Collector}, Subreddit: {Subreddit}, Page: {PageNumber}, PostsInPage: {PostCount}, HasNextPage: {HasNextPage}")]
    public static partial void LogCollectorSubredditPageFetched(this ILogger logger, string collector, string subreddit, int pageNumber, int postCount, bool hasNextPage);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Information, Message = "Subreddit page was empty. Collector: {Collector}, Subreddit: {Subreddit}, Page: {PageNumber}")]
    public static partial void LogCollectorSubredditPageEmpty(this ILogger logger, string collector, string subreddit, int pageNumber);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Information, Message = "Subreddit collection completed. Collector: {Collector}, Subreddit: {Subreddit}, PagesProcessed: {PagesProcessed}, YieldedPosts: {YieldedPosts}, YieldedComments: {YieldedComments}")]
    public static partial void LogCollectorSubredditCollectionCompleted(this ILogger logger, string collector, string subreddit, int pagesProcessed, int yieldedPosts, int yieldedComments);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Information, Message = "Subreddit page limit reached. Collector: {Collector}, Subreddit: {Subreddit}, ProcessedPages: {PageCount}, MaxPages: {MaxPages}")]
    public static partial void LogCollectorSubredditPageLimitReached(this ILogger logger, string collector, string subreddit, int pageCount, int? maxPages);

    // Verbose data-yield events.
    [LoggerMessage(EventId = 1100, Level = LogLevel.Debug, Message = "Post yielded. Collector: {Collector}, Subreddit: {Subreddit}, PostId: {PostId}, EstimatedComments: {EstimatedComments}")]
    public static partial void LogCollectorPostYielded(this ILogger logger, string collector, string subreddit, string? postId, int estimatedComments);

    [LoggerMessage(EventId = 1101, Level = LogLevel.Information, Message = "Comment page fetched. Collector: {Collector}, Subreddit: {Subreddit}, PostId: {PostId}, Page: {PageNumber}, CommentsInPage: {CommentsInPage}, HasNextPage: {HasNextPage}")]
    public static partial void LogCollectorCommentPageFetched(this ILogger logger, string collector, string subreddit, string? postId, int pageNumber, int commentsInPage, bool hasNextPage);

    [LoggerMessage(EventId = 1102, Level = LogLevel.Debug, Message = "Comment yielded. Collector: {Collector}, Subreddit: {Subreddit}, PostId: {PostId}, CommentId: {CommentId}")]
    public static partial void LogCollectorCommentYielded(this ILogger logger, string collector, string subreddit, string? postId, string? commentId);

    [LoggerMessage(EventId = 1103, Level = LogLevel.Information, Message = "Comment limit reached for post. Collector: {Collector}, Subreddit: {Subreddit}, PostId: {PostId}, YieldedComments: {YieldedComments}, MaxCommentsPerPost: {MaxCommentsPerPost}")]
    public static partial void LogCollectorCommentLimitReached(this ILogger logger, string collector, string subreddit, string? postId, int yieldedComments, int? maxCommentsPerPost);

    // HTTP and retry behavior.
    [LoggerMessage(EventId = 1200, Level = LogLevel.Debug, Message = "Collector HTTP request started. Collector: {Collector}, Url: {Url}, Attempt: {Attempt}/{MaxRetries}")]
    public static partial void LogCollectorHttpRequestStarted(this ILogger logger, string collector, string url, int attempt, int maxRetries);

    [LoggerMessage(EventId = 1201, Level = LogLevel.Debug, Message = "Collector HTTP request succeeded. Collector: {Collector}, Url: {Url}, StatusCode: {StatusCode}, ElapsedMs: {ElapsedMs}")]
    public static partial void LogCollectorHttpRequestSucceeded(this ILogger logger, string collector, string url, int statusCode, long elapsedMs);

    [LoggerMessage(EventId = 1202, Level = LogLevel.Warning, Message = "Collector HTTP request failed with status code. Collector: {Collector}, Url: {Url}, StatusCode: {StatusCode}, Attempt: {Attempt}/{MaxRetries}")]
    public static partial void LogCollectorHttpRequestFailedStatusCode(this ILogger logger, string collector, string url, int statusCode, int attempt, int maxRetries);

    [LoggerMessage(EventId = 1203, Level = LogLevel.Information, Message = "Collector API rate limit hit. Collector: {Collector}, Url: {Url}, Attempt: {Attempt}/{MaxRetries}, WaitSeconds: {RetryAfterSeconds}")]
    public static partial void LogCollectorHttpRequestRateLimited(this ILogger logger, string collector, string url, int attempt, int maxRetries, double retryAfterSeconds);

    [LoggerMessage(EventId = 1204, Level = LogLevel.Debug, Message = "Waiting before next collector HTTP attempt. Collector: {Collector}, Reason: {Reason}, DelayMs: {DelayMs}, NextAttempt: {NextAttempt}/{MaxRetries}")]
    public static partial void LogCollectorHttpRetryDelay(this ILogger logger, string collector, string reason, int delayMs, int nextAttempt, int maxRetries);

    // Failure events.
    [LoggerMessage(EventId = 1300, Level = LogLevel.Error, Message = "Unexpected error while fetching data from collector. Collector: {Collector}")]
    public static partial void LogCollectorRequestUnexpectedError(this ILogger logger, Exception ex, string collector);

    [LoggerMessage(EventId = 1301, Level = LogLevel.Error, Message = "Collector request failed after retries. Collector: {Collector}, MaxRetries: {MaxRetries}")]
    public static partial void LogCollectorRequestFailedAfterRetries(this ILogger logger, string collector, int maxRetries);
}
