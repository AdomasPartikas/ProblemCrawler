namespace ProblemCrawler.Core.Configuration;

/// <summary>
/// Runtime configuration for thread-level synthesis execution.
/// </summary>
public sealed class ThreadSynthesisConfiguration
{
    /// <summary>
    /// Number of threads to process per run iteration.
    /// </summary>
    public int BatchSize { get; set; } = 25;

    /// <summary>
    /// Maximum number of attempts per thread in a single execution.
    /// </summary>
    public int MaxAttemptsPerThread { get; set; } = 3;

    /// <summary>
    /// Maximum number of schema-repair retries when model output is invalid JSON.
    /// </summary>
    public int MaxRepairAttempts { get; set; } = 2;
}