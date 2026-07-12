using ProblemCrawler.Core.Records.Clustering;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Core.Interfaces
{
    public interface IClusterRepository
    {
        Task<IReadOnlyList<ClusterRun>> GetAllRunsAsync(CancellationToken cancellationToken);
        Task<IReadOnlyList<ProblemCluster>> GetClustersForRunAsync(Guid clusterRunId, CancellationToken cancellationToken);
        Task<IReadOnlyList<ClusterIdeaSummary>> GetIdeasForClusterAsync(Guid clusterRunId, int clusterId, CancellationToken cancellationToken);
        Task<bool> SetPinnedAsync(Guid clusterRunId, bool pinned, CancellationToken cancellationToken);
        Task<Guid?> GetLatestClusterRunIdAsync(CancellationToken ct);
        Task<IReadOnlyList<ClusterOpportunityScore>> GetOpportunityScoresAsync(Guid clusterRunId, CancellationToken ct);
        Task<IReadOnlyList<UmapPoint>> GetUmapPointsAsync(Guid clusterRunId, CancellationToken ct);
        Task<ClusterDetail?> GetClusterDetailAsync(Guid clusterRunId, int clusterId, CancellationToken ct);
        Task<bool> SaveValidationAsync(Guid clusterRunId, int clusterId, ClusterValidationInput input, CancellationToken ct);
    }
}
