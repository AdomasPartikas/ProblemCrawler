namespace ProblemCrawler.Core.Configuration;

/// <summary>
/// Runtime configuration for the LLM analysis stage.
/// </summary>
public sealed class LLMAnalysisConfiguration
{
    /// <summary>
    /// Number of records to process per run iteration.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Maximum number of attempts per item in a single execution.
    /// </summary>
    public int MaxAttemptsPerItem { get; set; } = 3;

    /// <summary>
    /// Maximum number of schema-repair retries when model output is invalid JSON.
    /// </summary>
    public int MaxRepairAttempts { get; set; } = 2;
}
