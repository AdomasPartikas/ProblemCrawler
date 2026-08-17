using Microsoft.Extensions.Logging;
using ProblemCrawler.Logging.EventIdConstants;

namespace ProblemCrawler.Logging.LogMessages;

public static partial class CollectorSchedulingLogMessages
{
    [LoggerMessage(EventId = CollectorSchedulingEventIdConstants.CollectorSchedulerSkippedConcurrentRunId, Level = LogLevel.Warning, Message = "Collector scheduler run skipped because a previous run is still in progress.")]
    public static partial void LogCollectorSchedulerSkippedConcurrentRun(this ILogger logger);

    [LoggerMessage(EventId = CollectorSchedulingEventIdConstants.CollectorSchedulerRunStartedId, Level = LogLevel.Information, Message = "Collector scheduler run started. Services to execute: {ServiceCount}")]
    public static partial void LogCollectorSchedulerRunStarted(this ILogger logger, int serviceCount);

    [LoggerMessage(EventId = CollectorSchedulingEventIdConstants.NoCollectionServicesRegisteredId, Level = LogLevel.Information, Message = "Collector scheduler run skipped because no collection services are registered.")]
    public static partial void LogNoCollectionServicesRegistered(this ILogger logger);

    [LoggerMessage(EventId = CollectorSchedulingEventIdConstants.CollectorServiceExecutionStartedId, Level = LogLevel.Information, Message = "Collector service execution started. Service: {ServiceName} ({CurrentIndex}/{TotalCount})")]
    public static partial void LogCollectorServiceExecutionStarted(this ILogger logger, string serviceName, int currentIndex, int totalCount);

    [LoggerMessage(EventId = CollectorSchedulingEventIdConstants.CollectorServiceExecutionCompletedId, Level = LogLevel.Information, Message = "Collector service execution completed. Service: {ServiceName}, collected items: {TotalItemsCollected}")]
    public static partial void LogCollectorServiceExecutionCompleted(this ILogger logger, string serviceName, int totalItemsCollected);

    [LoggerMessage(EventId = CollectorSchedulingEventIdConstants.CollectorSchedulerRunCompletedId, Level = LogLevel.Information, Message = "Collector scheduler run completed. Services executed: {ServiceCount}, total items collected: {TotalItemsCollected}")]
    public static partial void LogCollectorSchedulerRunCompleted(this ILogger logger, int serviceCount, int totalItemsCollected);
}
