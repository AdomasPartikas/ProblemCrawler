using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Core.Models;
using ProblemCrawler.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;

namespace ProblemCrawler.Infrastructure.Repositories
{
    /// <summary>
    /// Provides methods for managing collector items in the database.
    /// </summary>
    /// <param name="context">The database context used to access and modify collector items. Cannot be null.</param>
    public class CollectorItemRepository(
        ProblemCrawlerDbContext context) : ICollectorItemRepository
    {
        private readonly ProblemCrawlerDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
        /// <summary>
        /// Asynchronously inserts a batch of collector items into the database.
        /// </summary>
        /// <remarks>This method adds all items in the batch and commits them in a single transaction. If
        /// the operation is canceled, no items will be inserted.</remarks>
        /// <param name="items">The list of collector items to be inserted. Cannot be null. Each item will be added to the database in a
        /// single batch operation.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous insert operation.</returns>
        public async Task InsertBatchAsync(List<CollectorItem> items, CancellationToken cancellationToken)
        {
            await _context.CollectorItems.AddRangeAsync(items, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

    }
}
