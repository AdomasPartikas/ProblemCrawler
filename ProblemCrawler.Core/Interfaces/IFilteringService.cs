using ProblemCrawler.Core.Records.Filtering;

namespace ProblemCrawler.Core.Interfaces;

/// <summary>
/// Filters collected items and marks unusable content before analysis.
/// </summary>
public interface IFilteringService
{
    Task<FilteringRunSummary> ExecuteAsync(CancellationToken cancellationToken);
}
