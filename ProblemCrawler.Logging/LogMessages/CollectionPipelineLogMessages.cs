using Microsoft.Extensions.Logging;
using ProblemCrawler.Logging.EventIdConstants;

namespace ProblemCrawler.Logging.LogMessages;

public static partial class CollectionPipelineLogMessages
{
    [LoggerMessage(EventId = CollectionPipelineEventIdConstants.CollectionServiceStartedId, Level = LogLevel.Information, Message = "Collection service started for collector {CollectorName}")]
    public static partial void LogCollectionServiceStarted(this ILogger logger, string collectorName);

    [LoggerMessage(EventId = CollectionPipelineEventIdConstants.CollectionBatchPersistedId, Level = LogLevel.Debug, Message = "Collection batch persisted for {CollectorName}. Batch size: {BatchSize}, total persisted so far: {PersistedTotal}")]
    public static partial void LogCollectionBatchPersisted(this ILogger logger, string collectorName, int batchSize, int persistedTotal);

    [LoggerMessage(EventId = CollectionPipelineEventIdConstants.CollectedItemProcessingFailedId, Level = LogLevel.Warning, Message = "Failed to process collected item for {CollectorName}. SourceId: {SourceId}. Item skipped.")]
    public static partial void LogCollectedItemProcessingFailed(this ILogger logger, Exception exception, string collectorName, string? sourceId);

    [LoggerMessage(EventId = CollectionPipelineEventIdConstants.FinalBatchPersistedId, Level = LogLevel.Debug, Message = "Final collection batch persisted for {CollectorName}. Batch size: {BatchSize}, total persisted: {PersistedTotal}")]
    public static partial void LogFinalBatchPersisted(this ILogger logger, string collectorName, int batchSize, int persistedTotal);

    [LoggerMessage(EventId = CollectionPipelineEventIdConstants.CollectionServiceCompletedId, Level = LogLevel.Information, Message = "Collection service completed for {CollectorName}. Total items collected: {TotalItemsCollected}")]
    public static partial void LogCollectionServiceCompleted(this ILogger logger, string collectorName, int totalItemsCollected);
}
