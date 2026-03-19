namespace ProblemCrawler.Core.Configuration;

/// <summary>
/// Configuration for filtering rules and processing thresholds.
/// </summary>
public sealed class FilteringConfiguration
{
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
}
