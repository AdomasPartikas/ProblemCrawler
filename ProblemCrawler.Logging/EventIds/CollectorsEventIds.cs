using Microsoft.Extensions.Logging;
using ProblemCrawler.Logging.EventIdConstants;

namespace ProblemCrawler.Logging.EventIds;

public static class CollectorsEventIds
{
    public static readonly EventId NoSubredditsConfigured = new(CollectorsEventIdConstants.NoSubredditsConfiguredId, nameof(NoSubredditsConfigured));
    public static readonly EventId CollectorRunStarted = new(CollectorsEventIdConstants.CollectorRunStartedId, nameof(CollectorRunStarted));
    public static readonly EventId CollectorRunCompleted = new(CollectorsEventIdConstants.CollectorRunCompletedId, nameof(CollectorRunCompleted));
    public static readonly EventId SubredditCollectionStarted = new(CollectorsEventIdConstants.SubredditCollectionStartedId, nameof(SubredditCollectionStarted));
    public static readonly EventId SubredditCollectionCompleted = new(CollectorsEventIdConstants.SubredditCollectionCompletedId, nameof(SubredditCollectionCompleted));
    public static readonly EventId SubredditNoPostsFound = new(CollectorsEventIdConstants.SubredditNoPostsFoundId, nameof(SubredditNoPostsFound));
    public static readonly EventId CollectorPageLimitReached = new(CollectorsEventIdConstants.CollectorPageLimitReachedId, nameof(CollectorPageLimitReached));
    public static readonly EventId CommentLimitReached = new(CollectorsEventIdConstants.CommentLimitReachedId, nameof(CommentLimitReached));
}
