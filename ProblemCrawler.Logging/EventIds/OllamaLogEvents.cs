using Microsoft.Extensions.Logging;

namespace ProblemCrawler.Logging.EventIds;

public static class OllamaLogEvents
{
    public static readonly EventId RequestMetrics = new(3000, nameof(RequestMetrics));
    public static readonly EventId RequestFailed = new(3001, nameof(RequestFailed));
    public static readonly EventId RequestAttemptFailed = new(3002, nameof(RequestAttemptFailed));
    public static readonly EventId RequestFailedAfterRetries = new(3003, nameof(RequestFailedAfterRetries));
    public static readonly EventId StartupAttempt = new(3004, nameof(StartupAttempt));
    public static readonly EventId Started = new(3005, nameof(Started));
    public static readonly EventId StartupAttemptFailed = new(3006, nameof(StartupAttemptFailed));
    public static readonly EventId StartupFailed = new(3007, nameof(StartupFailed));
    public static readonly EventId ShuttingDown = new(3008, nameof(ShuttingDown));
    public static readonly EventId Stopped = new(3009, nameof(Stopped));
    public static readonly EventId VerificationFailed = new(3010, nameof(VerificationFailed));
    public static readonly EventId NoModelsReported = new(3011, nameof(NoModelsReported));
    public static readonly EventId VramUsage = new(3012, nameof(VramUsage));
    public static readonly EventId GpuActive = new(3013, nameof(GpuActive));
    public static readonly EventId RunningOnCpu = new(3014, nameof(RunningOnCpu));
    public static readonly EventId ProcessExited = new(3015, nameof(ProcessExited));
}