using ProblemCrawler.Core.Models;
using ProblemCrawler.Core.Records.Filtering;

namespace ProblemCrawler.Core.Interfaces;

public interface ICollectorItemRepository
{
    Task InsertBatchAsync(List<CollectorItem> items, CancellationToken cancellationToken);
    Task<IReadOnlyList<CollectorItemFilterCandidate>> GetFilteringCandidatesAsync(int batchSize, CancellationToken cancellationToken);
    Task UpdateAnalysisStagesAsync(IReadOnlyList<CollectorItemFilterUpdate> updates, CancellationToken cancellationToken);
}

