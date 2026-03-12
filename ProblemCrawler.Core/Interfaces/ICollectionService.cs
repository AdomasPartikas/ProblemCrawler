using ProblemCrawler.Core.Records.Reddit;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Core.Interfaces
{
    public interface ICollectionService
    {
       Task<(int total, List<CollectedItemResponse> items)> CollectAsync(CancellationToken cancellationToken);
    }
}
