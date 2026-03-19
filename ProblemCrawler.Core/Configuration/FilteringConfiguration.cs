namespace ProblemCrawler.Core.Configuration;

/// <summary>
/// Configuration for the scheduled filtering stage.
/// </summary>
public sealed class FilteringConfiguration
{
    /// <summary>
    /// Enables recurring filtering of collected items.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Cron expression used for the recurring filtering job.
    /// </summary>
    public string CronExpression { get; set; } = "5 * * * *";

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

    /// <summary>
    /// Number of database records to evaluate per iteration.
    /// </summary>
    public int BatchSize { get; set; } = 500;

    /// <summary>
    /// Minimum number of words required for content to be considered meaningful.
    /// </summary>
    public int MinimumWordCount { get; set; } = 2;

    /// <summary>
    /// Minimum number of alpha-numeric characters required for content.
    /// </summary>
    public int MinimumMeaningfulCharacters { get; set; } = 3;

    /// <summary>
    /// Exact marker values that should always be treated as deleted source content.
    /// </summary>
    public List<string> DeletedMarkers { get; set; } = ["[deleted]", "[removed]"];

    /// <summary>
    /// Low-value responses that should be marked as removed.
    /// </summary>
    public List<string> RemovedWordList { get; set; } =
    [
        "k",
        "ok",
        "okay",
        "yes",
        "no",
        "same",
        "lol",
        "lmao",
        "idk",
        "thx",
        "thanks",
        "following",
        "+1"
    ];
}
