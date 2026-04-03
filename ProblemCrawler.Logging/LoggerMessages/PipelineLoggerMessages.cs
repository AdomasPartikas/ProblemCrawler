using Microsoft.Extensions.Logging;

namespace ProblemCrawler.Logging.LoggerMessages;

public static partial class PipelineLoggerMessages
{
    // Successful run summaries.
    [LoggerMessage(EventId = 2000, Level = LogLevel.Information, Message = "Collection completed. Total items: {Total}")]
    public static partial void LogCollectionCompleted(this ILogger logger, int total);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Scheduled collection run completed. Total items: {Total}")]
    public static partial void LogScheduledCollectionCompleted(this ILogger logger, int total);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "Filtering completed. Evaluated: {Evaluated}, Ready: {Ready}, Removed: {Removed}, Deleted: {Deleted}, Updated: {Updated}")]
    public static partial void LogFilteringCompleted(this ILogger logger, int evaluated, int ready, int removed, int deleted, int updated);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Information, Message = "LLM analysis completed. Evaluated: {Evaluated}, Analysed: {Analysed}, Skipped: {Skipped}, Failed: {Failed}")]
    public static partial void LogLlmAnalysisCompleted(this ILogger logger, int evaluated, int analysed, int skipped, int failed);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Information, Message = "Thread synthesis completed. Evaluated: {Evaluated}, Synthesized: {Synthesized}, Skipped: {Skipped}, Failed: {Failed}")]
    public static partial void LogThreadSynthesisCompleted(this ILogger logger, int evaluated, int synthesized, int skipped, int failed);
}
