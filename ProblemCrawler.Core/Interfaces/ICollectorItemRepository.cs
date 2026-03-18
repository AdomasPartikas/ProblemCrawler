using ProblemCrawler.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Core.Interfaces
{
    public interface ICollectorItemRepository
    {
        Task InsertBatchAsync(List<CollectorItem> items, CancellationToken cancellationToken);
    }
}
