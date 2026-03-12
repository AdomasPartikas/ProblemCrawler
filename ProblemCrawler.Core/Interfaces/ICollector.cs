namespace ProblemCrawler.Core.Interfaces;

/// <summary>
/// Represents a source of problem/opportunity data (e.g., Reddit, Twitter, StackOverflow).
/// Collectors are responsible for fetching raw data from their source.
/// </summary>
public interface ICollector
{
    /// <summary>
    /// Gets the name of this collector (e.g., "Reddit")
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gathers raw data from the source.
    /// Yields items as they are collected, allowing for streaming/pipeline processing.
    /// </summary>
    /// <param name="cancellationToken">Token to signal cancellation</param>
    /// <returns>An async enumerable of raw collected items</returns>
    IAsyncEnumerable<ICollectorItem> GatherAsync(CancellationToken cancellationToken = default);
}