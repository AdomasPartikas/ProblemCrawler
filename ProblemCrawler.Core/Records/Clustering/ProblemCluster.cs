using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Core.Records.Clustering
{
    public record ProblemCluster(
        Guid Id,
        int ClusterId,
        Guid ClusterRunId,
        int Size,
        float AvgConfidence,
        DateTime CreatedAtUtc);
}
