namespace ProblemCrawler.Logging.EventIdConstants;

public static class CollectionPipelineEventIdConstants
{
    // Collection pipeline events (300-329)
    public const int CollectionServiceStartedId = 300;
    public const int CollectionBatchPersistedId = 301;
    public const int CollectedItemProcessingFailedId = 302;
    public const int FinalBatchPersistedId = 303;
    public const int CollectionServiceCompletedId = 304;
}