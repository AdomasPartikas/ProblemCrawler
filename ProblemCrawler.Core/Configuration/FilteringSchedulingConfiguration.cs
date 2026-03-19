namespace ProblemCrawler.Core.Configuration;

/// <summary>
/// Scheduling configuration for filtering execution.
/// </summary>
public sealed class FilteringSchedulingConfiguration
{
    /// <summary>
    /// Enables recurring scheduling of the filtering stage.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Cron expression used for the recurring filtering job.
    /// </summary>
    public string CronExpression { get; set; } = "*/30 * * * *";

    /// <summary>
    /// Time zone identifier used when evaluating the cron expression.
    /// </summary>
    public string TimeZoneId { get; set; } = "UTC";

    /// <summary>
    /// Queues one filtering run when the application starts.
    /// </summary>
    public bool RunOnStartup { get; set; }

    /// <summary>
    /// Allows overlapping runs when the schedule fires before the previous run finishes.
    /// </summary>
    public bool AllowConcurrentRuns { get; set; }
}
