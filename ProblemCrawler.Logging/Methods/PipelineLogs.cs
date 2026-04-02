using Microsoft.Extensions.Logging;
using ProblemCrawler.Logging.EventIds;

namespace ProblemCrawler.Logging.Methods;

public static class PipelineLogs
{
    public static void LogCollectionCompleted(this ILogger logger, int total) =>
        logger.LogInformation(PipelineLogEvents.CollectionCompleted, "Collection completed. Total items: {Total}", total);

    public static void LogScheduledCollectionCompleted(this ILogger logger, int total) =>
        logger.LogInformation(PipelineLogEvents.ScheduledCollectionCompleted, "Scheduled collection run completed. Total items: {Total}", total);

    public static void LogFilteringCompleted(this ILogger logger, int evaluated, int ready, int removed, int deleted, int updated) =>
        logger.LogInformation(
            PipelineLogEvents.FilteringCompleted,
            "Filtering completed. Evaluated: {Evaluated}, ready: {Ready}, removed: {Removed}, deleted: {Deleted}, updated: {Updated}",
            evaluated,
            ready,
            removed,
            deleted,
            updated);

    public static void LogLlmAnalysisCompleted(this ILogger logger, int evaluated, int analysed, int skipped, int failed) =>
        logger.LogInformation(
            PipelineLogEvents.LlmAnalysisCompleted,
            "LLM analysis completed. Evaluated: {Evaluated}, analysed: {Analysed}, skipped: {Skipped}, failed: {Failed}",
            evaluated,
            analysed,
            skipped,
            failed);

    public static void LogThreadSynthesisCompleted(this ILogger logger, int evaluated, int synthesized, int skipped, int failed) =>
        logger.LogInformation(
            PipelineLogEvents.ThreadSynthesisCompleted,
            "Thread synthesis completed. Evaluated: {Evaluated}, synthesized: {Synthesized}, skipped: {Skipped}, failed: {Failed}",
            evaluated,
            synthesized,
            skipped,
            failed);
}