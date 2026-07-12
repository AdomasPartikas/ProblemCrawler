using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Core.Records.Clustering
{
    public record OpportunitiesResponse(
        Guid ClusterRunId,
        IReadOnlyList<ClusterOpportunityScore> Clusters);

    public record AtlasResponse(
        Guid ClusterRunId,
        IReadOnlyList<UmapPoint> Points);

    public record ClusterDetailResponse(
        Guid ClusterRunId,
        ClusterDetail Cluster);
}
