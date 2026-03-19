using ProblemCrawler.Core.Records.Reddit;

namespace ProblemCrawler.Core.Interfaces;

public interface ICollectionService
{
    Task<(int total, List<CollectedItemResponse> items)> CollectAsync(CancellationToken cancellationToken);
}

