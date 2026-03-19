using ProblemCrawler.Core.Models;

namespace ProblemCrawler.Core.Interfaces;

public interface ICollectorItemRepository
{
    Task InsertBatchAsync(List<CollectorItem> items, CancellationToken cancellationToken);
}

