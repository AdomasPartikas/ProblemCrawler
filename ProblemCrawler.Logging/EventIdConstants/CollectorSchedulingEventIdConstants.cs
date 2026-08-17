namespace ProblemCrawler.Logging.EventIdConstants;

public static class CollectorSchedulingEventIdConstants
{
    // Collector scheduling events (400-429)
    public const int CollectorSchedulerSkippedConcurrentRunId = 400;
    public const int CollectorSchedulerRunStartedId = 401;
    public const int NoCollectionServicesRegisteredId = 402;
    public const int CollectorServiceExecutionStartedId = 403;
    public const int CollectorServiceExecutionCompletedId = 404;
    public const int CollectorSchedulerRunCompletedId = 405;
}