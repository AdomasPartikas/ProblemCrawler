namespace ProblemCrawler.Pipeline.Interfaces;

/// <summary>
/// Executes one filtering run against collected items.
/// </summary>
public interface IFilteringSchedulerTask
{
    public Task ExecuteAsync();
}
