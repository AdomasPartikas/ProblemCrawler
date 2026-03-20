using ProblemCrawler.Core.Models;
using ProblemCrawler.Core.Records.Filtering;
using ProblemCrawler.Core.Records.LLM;

namespace ProblemCrawler.Core.Interfaces;

public interface ICollectorItemRepository
{
    Task InsertBatchAsync(List<CollectorItem> items, CancellationToken cancellationToken);
    Task<IReadOnlyList<CollectorItemFilterCandidate>> GetFilteringCandidatesAsync(int batchSize, CancellationToken cancellationToken);
    Task UpdateAnalysisStagesAsync(IReadOnlyList<CollectorItemFilterUpdate> updates, CancellationToken cancellationToken);
    Task<IReadOnlyList<LLMAnalysisCandidate>> GetLlmAnalysisCandidatesAsync(int batchSize, CancellationToken cancellationToken);
    Task<LLMAnalysisContext?> GetLlmAnalysisContextAsync(Guid collectorItemId, CancellationToken cancellationToken);
    Task<LLMAnalysisCandidate?> GetLlmAnalysisCandidateByIdAsync(Guid collectorItemId, CancellationToken cancellationToken);
    Task UpsertAnalysedItemAsync(AnalysedItemUpsert analysis, CancellationToken cancellationToken);
}

