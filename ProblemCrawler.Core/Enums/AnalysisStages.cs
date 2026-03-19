namespace ProblemCrawler.Core.Enums;

public enum AnalysisStages
{
    /// <summary>
    /// New pending to be processed
    /// </summary>
    New,
    /// <summary>
    /// Filtered out
    /// </summary>
    Removed,
    /// <summary>
    /// Deleted due to being removed or corrupted
    /// </summary>
    Deleted,
    /// <summary>
    /// Processed and analysed
    /// </summary>
    Analysed
}