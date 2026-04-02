namespace ProblemCrawler.Core.Configuration;

/// <summary>
/// Scheduling configuration for thread synthesis execution.
/// </summary>
public sealed class ThreadSynthesisSchedulingConfiguration
{
    /// <summary>
    /// Enables recurring scheduling of the thread synthesis stage.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Cron expression used for the recurring thread synthesis job.
    /// </summary>
    public string CronExpression { get; set; } = "0 6 * * *";

    /// <summary>
    /// Time zone identifier used when evaluating the cron expression.
    /// </summary>
    public string TimeZoneId { get; set; } = "UTC";

    /// <summary>
    /// Queues one synthesis run when the application starts.
    /// </summary>
    public bool RunOnStartup { get; set; }

    /// <summary>
    /// Allows overlapping runs when the schedule fires before the previous run finishes.
    /// </summary>
    public bool AllowConcurrentRuns { get; set; }
}