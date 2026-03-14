namespace ProblemCrawler.Core.Configuration;

/// <summary>
/// Scheduling configuration for collector execution.
/// </summary>
public sealed class CollectorSchedulingConfiguration
{
    /// <summary>
    /// Enables recurring scheduling of collectors.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Cron expression used for the recurring collector job.
    /// </summary>
    public string CronExpression { get; set; } = "0 * * * *";

    /// <summary>
    /// Time zone identifier used when evaluating the cron expression.
    /// </summary>
    public string TimeZoneId { get; set; } = "UTC";

    /// <summary>
    /// Queues one collection run when the application starts.
    /// </summary>
    public bool RunOnStartup { get; set; }

    /// <summary>
    /// Allows overlapping runs when the schedule fires before the previous run finishes.
    /// </summary>
    public bool AllowConcurrentRuns { get; set; }
}