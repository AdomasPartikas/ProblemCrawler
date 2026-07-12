using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Core.Records.Clustering
{
    public record UmapPoint(
        Guid EmbeddingId,
        int ClusterId,
        float X,
        float Y,
        float Confidence,
        string ProblemSummary);
}
