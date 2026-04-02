namespace ProblemCrawler.Core.Configuration;

/// <summary>
/// Scheduling configuration for LLM analysis execution.
/// </summary>
public sealed class LLMAnalysisSchedulingConfiguration
{
    /// <summary>
    /// Enables recurring scheduling of the LLM analysis stage.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Cron expression used for the recurring LLM analysis job.
    /// </summary>
    public string CronExpression { get; set; } = "0 0 * * *";

    /// <summary>
    /// Time zone identifier used when evaluating the cron expression.
    /// </summary>
    public string TimeZoneId { get; set; } = "UTC";

    /// <summary>
    /// Queues one LLM analysis run when the application starts.
    /// </summary>
    public bool RunOnStartup { get; set; }

    /// <summary>
    /// Allows overlapping runs when the schedule fires before the previous run finishes.
    /// </summary>
    public bool AllowConcurrentRuns { get; set; }
}
