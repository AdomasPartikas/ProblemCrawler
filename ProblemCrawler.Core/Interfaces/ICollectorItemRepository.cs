using ProblemCrawler.Core.Models;
using ProblemCrawler.Core.Records.Clustering;
using ProblemCrawler.Core.Records.Embedding;
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
    Task UpsertAnalysedItemsBatchAsync(IReadOnlyList<AnalysedItemUpsert> analyses, CancellationToken cancellationToken);
    Task<IReadOnlyList<ThreadSynthesisCandidate>> GetThreadSynthesisCandidatesAsync(int batchSize, CancellationToken cancellationToken);
    Task<ThreadSynthesisContext?> GetThreadSynthesisContextAsync(Guid rootCollectorItemId, CancellationToken cancellationToken);
    Task UpsertThreadSynthesisAsync(ThreadSynthesisUpsert synthesis, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmbeddingCandidate>> GetEmbeddingCandidatesAsync(int batchSize, string model, CancellationToken cancellationToken);
    Task UpsertEmbeddingAsync(IReadOnlyList<EmbeddingUpsert> upserts, CancellationToken cancellationToken);
    Task ReleaseSynthesisClaimAsync(Guid rootCollectorItemId, CancellationToken cancellationToken);
}

