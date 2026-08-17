using Microsoft.Extensions.Logging;
using ProblemCrawler.Logging.EventIdConstants;

namespace ProblemCrawler.Logging.EventIds;

public static class CollectorSchedulingEventIds
{
    public static readonly EventId CollectorSchedulerSkippedConcurrentRun = new(CollectorSchedulingEventIdConstants.CollectorSchedulerSkippedConcurrentRunId, nameof(CollectorSchedulerSkippedConcurrentRun));
    public static readonly EventId CollectorSchedulerRunStarted = new(CollectorSchedulingEventIdConstants.CollectorSchedulerRunStartedId, nameof(CollectorSchedulerRunStarted));
    public static readonly EventId NoCollectionServicesRegistered = new(CollectorSchedulingEventIdConstants.NoCollectionServicesRegisteredId, nameof(NoCollectionServicesRegistered));
    public static readonly EventId CollectorServiceExecutionStarted = new(CollectorSchedulingEventIdConstants.CollectorServiceExecutionStartedId, nameof(CollectorServiceExecutionStarted));
    public static readonly EventId CollectorServiceExecutionCompleted = new(CollectorSchedulingEventIdConstants.CollectorServiceExecutionCompletedId, nameof(CollectorServiceExecutionCompleted));
    public static readonly EventId CollectorSchedulerRunCompleted = new(CollectorSchedulingEventIdConstants.CollectorSchedulerRunCompletedId, nameof(CollectorSchedulerRunCompleted));
}
