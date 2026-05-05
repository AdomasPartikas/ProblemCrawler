using ProblemCrawler.Core.Records.Embedding;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Core.Interfaces
{
    public interface IIdeaEmbeddingService
    {
        Task<EmbeddingRunSummary> ExecuteAsync(CancellationToken cancellationToken);
    }
}
