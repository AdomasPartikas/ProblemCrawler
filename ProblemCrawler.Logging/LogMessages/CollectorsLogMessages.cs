using Microsoft.Extensions.Logging;
using ProblemCrawler.Logging.EventIdConstants;
using ProblemCrawler.Logging.EventIds;

namespace ProblemCrawler.Logging.LogMessages;

public static partial class CollectorsLogMessages
{
    [LoggerMessage(EventId = CollectorsEventIdConstants.NoSubredditsConfiguredId, Level = LogLevel.Warning, Message = "Collector run skipped because no subreddits are configured.")]
    public static partial void LogNoSubredditsConfigured(this ILogger logger);

    [LoggerMessage(EventId = CollectorsEventIdConstants.CollectorRunStartedId, Level = LogLevel.Information, Message = "Collector run started. Subreddits: {SubredditCount}, fetch comments: {FetchComments}")]
    public static partial void LogCollectorRunStarted(this ILogger logger, int subredditCount, bool fetchComments);

    [LoggerMessage(EventId = CollectorsEventIdConstants.CollectorRunCompletedId, Level = LogLevel.Information, Message = "Collector run completed. Subreddits processed: {SubredditCount}")]
    public static partial void LogCollectorRunCompleted(this ILogger logger, int subredditCount);

    [LoggerMessage(EventId = CollectorsEventIdConstants.SubredditCollectionStartedId, Level = LogLevel.Information, Message = "Subreddit collection started for {Subreddit}")]
    public static partial void LogSubredditCollectionStarted(this ILogger logger, string subreddit);

    [LoggerMessage(EventId = CollectorsEventIdConstants.SubredditCollectionCompletedId, Level = LogLevel.Information, Message = "Subreddit collection completed for {Subreddit}. Pages fetched: {PageCount}")]
    public static partial void LogSubredditCollectionCompleted(this ILogger logger, string subreddit, int pageCount);

    [LoggerMessage(EventId = CollectorsEventIdConstants.SubredditNoPostsFoundId, Level = LogLevel.Debug, Message = "No posts found for {Subreddit} at page index {PageIndex}")]
    public static partial void LogSubredditNoPostsFound(this ILogger logger, string subreddit, int pageIndex);

    [LoggerMessage(EventId = CollectorsEventIdConstants.CollectorPageLimitReachedId, Level = LogLevel.Debug, Message = "Page limit reached for {Subreddit}. Max pages: {MaxPages}")]
    public static partial void LogCollectorPageLimitReached(this ILogger logger, string subreddit, int maxPages);

    [LoggerMessage(EventId = CollectorsEventIdConstants.CommentLimitReachedId, Level = LogLevel.Debug, Message = "Comment limit reached for post {PostId} in {Subreddit}. Max comments: {MaxComments}")]
    public static partial void LogCommentLimitReached(this ILogger logger, string postId, string subreddit, int maxComments);
}
