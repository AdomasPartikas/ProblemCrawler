using ProblemCrawler.Core.Records.LLM;

namespace ProblemCrawler.Core.Interfaces;

public interface IThreadSynthesisService
{
    Task<ThreadSynthesisRunSummary> ExecuteAsync(CancellationToken cancellationToken);
    Task<ThreadSynthesisExecutionResult> ExecuteForThreadAsync(Guid rootCollectorItemId, ThreadSynthesisContext context, ICollectorItemRepository repository, CancellationToken cancellationToken);
}