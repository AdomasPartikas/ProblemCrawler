using Microsoft.Extensions.Logging;
using ProblemCrawler.Logging.EventIdConstants;

namespace ProblemCrawler.Logging.EventIds;

public static class CollectionPipelineEventIds
{
    public static readonly EventId CollectionServiceStarted = new(CollectionPipelineEventIdConstants.CollectionServiceStartedId, nameof(CollectionServiceStarted));
    public static readonly EventId CollectionBatchPersisted = new(CollectionPipelineEventIdConstants.CollectionBatchPersistedId, nameof(CollectionBatchPersisted));
    public static readonly EventId CollectedItemProcessingFailed = new(CollectionPipelineEventIdConstants.CollectedItemProcessingFailedId, nameof(CollectedItemProcessingFailed));
    public static readonly EventId FinalBatchPersisted = new(CollectionPipelineEventIdConstants.FinalBatchPersistedId, nameof(FinalBatchPersisted));
    public static readonly EventId CollectionServiceCompleted = new(CollectionPipelineEventIdConstants.CollectionServiceCompletedId, nameof(CollectionServiceCompleted));
}
