using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Core.Records.Clustering
{
    public record ClusterIdeaSummary(
        Guid ThreadSynthesizedIdeaId,
        int ClusterId,
        float? ClusterConfidence,
        string IdeaSnapshot,
        DateTime CreatedAtUtc);
}
