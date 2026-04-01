namespace ProblemCrawler.Pipeline.Interfaces;

/// <summary>
/// Executes one LLM analysis run against ready candidates.
/// </summary>
public interface ILLMAnalysisSchedulerTask
{
    Task ExecuteAsync();
}
