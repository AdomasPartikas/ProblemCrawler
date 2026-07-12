using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Core.Records.Clustering
{
    public record ClusterDetail(
        int ClusterId,
        string Label,
        string? Description,
        string? Opportunity,
        int Size,
        float AvgConfidence,
        IReadOnlyList<ClusterIdeaSummary> Ideas,
        string? ValidationAction,
        IReadOnlyList<int>? MergedFromClusterIds);
}
