using ProblemCrawler.Core.Records.LLM;

namespace ProblemCrawler.Core.Interfaces;

public interface ILLMAnalysisService
{
    Task<LLMAnalysisRunSummary> ExecuteAsync(CancellationToken cancellationToken);
    Task<LLMAnalysisExecutionResult> ExecuteForItemAsync(Guid collectorItemId, CancellationToken cancellationToken);
}
