using Microsoft.EntityFrameworkCore;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Core.Records.Clustering;
using ProblemCrawler.Infrastructure.Data;


namespace ProblemCrawler.Infrastructure.Repositories
{
    public sealed class ClusterRepository(ProblemCrawlerDbContext db) : IClusterRepository
    {
        private readonly ProblemCrawlerDbContext _db = db;

        public async Task<IReadOnlyList<ClusterRun>> GetAllRunsAsync(CancellationToken cancellationToken)
        {
            return await _db.ClusterRuns
                .OrderByDescending(r => r.CreatedAtUtc)
                .Select(r => new ClusterRun(
                    r.Id,
                    r.Algorithm,
                    r.MinClusterSize,
                    r.MinSamples,
                    r.IsPinned,
                    r.ProblemClusters.Count,
                    r.ProblemClusters.Sum(c => c.Size),
                    r.CreatedAtUtc))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ProblemCluster>> GetClustersForRunAsync(Guid clusterRunId, CancellationToken cancellationToken)
        {
            return await _db.ProblemClusters
                .Where(c => c.ClusterRunId == clusterRunId)
                .OrderBy(c => c.ClusterId)
                .Select(c => new ProblemCluster(
                    c.Id,
                    c.ClusterId,
                    c.ClusterRunId,
                    c.Size,
                    c.AvgConfidence,
                    c.CreatedAtUtc))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ClusterIdeaSummary>> GetIdeasForClusterAsync(Guid clusterRunId, int clusterId, CancellationToken cancellationToken)
        {
            return await _db.IdeaEmbeddingArchives
                .Where(a => a.ClusterRunId == clusterRunId && a.ClusterId == clusterId)
                .Select(a => new ClusterIdeaSummary(
                    a.ThreadSynthesizedIdeaId,
                    a.ClusterId!.Value,
                    a.ClusterConfidence,
                    a.IdeaSnapshot,
                    a.CreatedAtUtc))
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> SetPinnedAsync(Guid clusterRunId, bool pinned, CancellationToken cancellationToken)
        {
            var run = await _db.ClusterRuns.FindAsync([clusterRunId], cancellationToken);
            if (run is null) return false;

            run.IsPinned = pinned;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
