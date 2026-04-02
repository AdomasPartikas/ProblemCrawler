using Microsoft.Extensions.Logging;

namespace ProblemCrawler.Logging.EventIds;

public static class PipelineLogEvents
{
    public static readonly EventId CollectionCompleted = new(2000, nameof(CollectionCompleted));
    public static readonly EventId ScheduledCollectionCompleted = new(2001, nameof(ScheduledCollectionCompleted));
    public static readonly EventId FilteringCompleted = new(2002, nameof(FilteringCompleted));
    public static readonly EventId LlmAnalysisCompleted = new(2003, nameof(LlmAnalysisCompleted));
    public static readonly EventId ThreadSynthesisCompleted = new(2004, nameof(ThreadSynthesisCompleted));
}