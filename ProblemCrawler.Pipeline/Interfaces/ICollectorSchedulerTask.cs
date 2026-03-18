namespace ProblemCrawler.Pipeline.Interfaces;

/// <summary>
/// Executes one collection run across all registered collectors.
/// </summary>
public interface ICollectorSchedulerTask
{
    public Task ExecuteAsync();
}