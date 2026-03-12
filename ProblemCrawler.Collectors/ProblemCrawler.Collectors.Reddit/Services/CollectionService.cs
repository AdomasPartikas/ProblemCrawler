using Microsoft.Extensions.Logging;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Core.Models;
using ProblemCrawler.Core.Records.Reddit;
using ProblemCrawler.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Collectors.Reddit.Services
{
    /// <summary>
    /// Provides functionality to collect items from a source, log collection events, and persist collected items in
    /// batches.
    /// </summary>
    /// <remarks>CollectionService batches collected items for efficient storage and logs each collection
    /// event. The service is intended for scenarios where items are gathered asynchronously and persisted in
    /// bulk.</remarks>
    /// <param name="collector">The collector used to gather items from the source. Cannot be null.</param>
    /// <param name="repository">The repository used to store collected items. Cannot be null.</param>
    /// <param name="logger">The logger used to record collection events and operational information. Cannot be null.</param>
    public class CollectionService(ICollector collector, ICollectorItemRepository repository, ILogger<CollectionService> logger) : ICollectionService
    {
        private readonly ICollector _collector = collector ?? throw new ArgumentNullException(nameof(collector));
        private readonly ILogger<CollectionService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ICollectorItemRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));


        /// <summary>
        /// Asynchronously collects items from the underlying collector and stores them in batches. Returns the total
        /// number of collected items and their corresponding response objects.
        /// </summary>
        /// <remarks>Items are inserted into the repository in batches to optimize performance. Logging is
        /// performed for each collected item. The operation can be cancelled at any time using the provided
        /// cancellation token.</remarks>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the collection operation.</param>
        /// <returns>A tuple containing the total number of collected items and a list of response objects representing each
        /// collected item. The list will be empty if no items are collected.</returns>
        public async Task<(int total, List<CollectedItemResponse> items)> CollectAsync(
            CancellationToken cancellationToken)
        {
            const int batchSize = 200;
            var buffer = new List<CollectorItem>();
            var responses = new List<CollectedItemResponse>();

            await foreach (var item in _collector.GatherAsync(cancellationToken))
            {
                _logger.LogInformation(
                    "Collected {ItemType} {ItemId} from {Source} by {Author}",
                    item.ItemType,
                    item.Id,
                    item.Source,
                    item.Author);
                buffer.Add((CollectorItem)item);
                responses.Add(new CollectedItemResponse(
                    item.Id,
                    item.ItemType,
                    item.Author,
                    item.CreatedAt,
                    item.SourceUrl));
                    
                if(buffer.Count >= batchSize)
                {
                    await _repository.InsertBatchAsync(buffer, cancellationToken);
                    buffer.Clear();
                }
            }
            if(buffer.Count > 0)
            {
                await _repository.InsertBatchAsync(buffer, cancellationToken);
                buffer.Clear();
            }

            return(responses.Count,responses);

        }
    }
    
}
