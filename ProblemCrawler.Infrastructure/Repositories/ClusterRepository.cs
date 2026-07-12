using Microsoft.EntityFrameworkCore;
using ProblemCrawler.Core.Enums;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Core.Records.Clustering;
using ProblemCrawler.Infrastructure.Data;
using ProblemCrawler.Infrastructure.Entities;
using System.Text.Json;

namespace ProblemCrawler.Infrastructure.Repositories
{
    public sealed class ClusterRepository(ProblemCrawlerDbContext db) : IClusterRepository
    {
        private readonly ProblemCrawlerDbContext _db = db;

        private static readonly JsonSerializerOptions SnapshotJsonOptions =
            new(JsonSerializerDefaults.Web);

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

        public async Task<Guid?> GetLatestClusterRunIdAsync(CancellationToken ct) =>
            await _db.ClusterRuns
                .OrderByDescending(r => r.CreatedAtUtc)
                .Select(r => (Guid?)r.Id)
                .FirstOrDefaultAsync(ct);

        public async Task<IReadOnlyList<ClusterOpportunityScore>> GetOpportunityScoresAsync(
            Guid clusterRunId, CancellationToken ct)
        {
            var validations = await _db.ClusterValidations
                .AsNoTracking()
                .Where(v => v.ClusterRunId == clusterRunId)
                .ToListAsync(ct);

            var discardedIds = validations
                .Where(v => v.Action == ClusterValidationActions.Discard)
                .Select(v => v.ClusterId)
                .ToHashSet();

            var mergeSourceToTarget = validations
                .Where(v => v.Action == ClusterValidationActions.Merge && v.MergeTargetClusterId.HasValue)
                .ToDictionary(v => v.ClusterId, v => v.MergeTargetClusterId!.Value);

            var validationByCluster = validations.ToDictionary(v => v.ClusterId);

            var isCurrentRun = await _db.ThreadSynthesizedIdeasEmbedding
                .AnyAsync(e => e.ClusterRunId == clusterRunId, ct);

            List<IdeaScoreInput> ideas;

            if (isCurrentRun)
            {
                ideas = await _db.ThreadSynthesizedIdeasEmbedding
                    .AsNoTracking()
                    .Where(e => e.ClusterRunId == clusterRunId && e.ClusterId != null && e.ClusterId != -1)
                    .Select(e => new IdeaScoreInput(
                        e.ClusterId!.Value,
                        e.ClusterConfidence,
                        e.Idea.UrgencySignal,
                        e.Idea.SoftwareOpportunity,
                        e.Idea.CurrentWorkaround != null,
                        e.Idea.SupportingMentionCount,
                        e.Idea.SupportingDistinctAuthorCount))
                    .ToListAsync(ct);
            }
            else
            {
                var archived = await _db.IdeaEmbeddingArchives
                    .AsNoTracking()
                    .Where(a => a.ClusterRunId == clusterRunId && a.ClusterId != null && a.ClusterId != -1)
                    .Select(a => new { a.ClusterId, a.ClusterConfidence, a.IdeaSnapshot })
                    .ToListAsync(ct);

                ideas = archived
                    .Select(a =>
                    {
                        var snap = JsonSerializer.Deserialize<IdeaSnapshotDto>(a.IdeaSnapshot, SnapshotJsonOptions)
                                   ?? new IdeaSnapshotDto();
                        return new IdeaScoreInput(
                            a.ClusterId!.Value,
                            a.ClusterConfidence,
                            snap.UrgencySignal,
                            snap.SoftwareOpportunity,
                            snap.CurrentWorkaround != null,
                            snap.SupportingMentionCount,
                            snap.SupportingDistinctAuthorCount);
                    })
                    .ToList();
            }

            if (ideas.Count == 0) return [];

            // Remap merge sources → their targets, then filter out discarded clusters
            var processedIdeas = ideas
                .Select(i => mergeSourceToTarget.TryGetValue(i.ClusterId, out var t) ? i with { ClusterId = t } : i)
                .Where(i => !discardedIds.Contains(i.ClusterId))
                .ToList();

            if (processedIdeas.Count == 0) return [];

            var maxMentions = processedIdeas.Max(i => i.MentionCount);
            var maxAuthors = processedIdeas.Max(i => i.AuthorCount);

            var grouped = processedIdeas
                .GroupBy(i => i.ClusterId)
                .Select(g => new
                {
                    ClusterId = g.Key,
                    PainIntensityScore = g.Average(i => MapUrgency(i.UrgencySignal)),
                    MentionFrequencyScore = Normalize(g.Sum(i => i.MentionCount), maxMentions),
                    SolutionVacuumScore = g.Average(i => i.HasWorkaround ? 1.0 : 0.0),
                    SoftwareMarketScore = g.Average(i => i.SoftwareOpportunity ? 1.0 : 0.0),
                    AuthorBreadthScore = Normalize(g.Average(i => i.AuthorCount), maxAuthors),
                    AvgConfidence = g.Average(i => i.Confidence ?? 0f),
                })
                .ToList();

            var clusters = await _db.ProblemClusters
                .AsNoTracking()
                .Where(c => c.ClusterRunId == clusterRunId)
                .ToListAsync(ct);

            var clusterMap = clusters.ToDictionary(c => c.ClusterId);

            // Extra size added to merge targets from their merged-in sources
            var mergeTargetExtraSizes = clusters
                .Where(c => mergeSourceToTarget.ContainsKey(c.ClusterId))
                .GroupBy(c => mergeSourceToTarget[c.ClusterId])
                .ToDictionary(g => g.Key, g => g.Sum(c => c.Size));

            return grouped
                .Where(g => clusterMap.ContainsKey(g.ClusterId))
                .Select(g =>
                {
                    var c = clusterMap[g.ClusterId];
                    var extraSize = mergeTargetExtraSizes.TryGetValue(g.ClusterId, out var extra) ? extra : 0;
                    var validationAction = validationByCluster.TryGetValue(g.ClusterId, out var v) ? v.Action.ToString() : null;

                    var score = 0.30 * g.PainIntensityScore
                              + 0.20 * g.MentionFrequencyScore
                              + 0.20 * g.SolutionVacuumScore
                              + 0.15 * g.SoftwareMarketScore
                              + 0.15 * g.AuthorBreadthScore;

                    return new ClusterOpportunityScore(
                        g.ClusterId,
                        c.Label ?? $"Cluster {g.ClusterId}",
                        c.Description,
                        c.Opportunity,
                        c.Size + extraSize,
                        (float)g.AvgConfidence,
                        Math.Round(g.PainIntensityScore, 4),
                        Math.Round(g.MentionFrequencyScore, 4),
                        Math.Round(g.SolutionVacuumScore, 4),
                        Math.Round(g.SoftwareMarketScore, 4),
                        Math.Round(g.AuthorBreadthScore, 4),
                        Math.Round(score, 4),
                        validationAction
                    );
                })
                .OrderByDescending(r => r.OpportunityScore)
                .ToList();
        }

        public async Task<IReadOnlyList<UmapPoint>> GetUmapPointsAsync(Guid clusterRunId, CancellationToken ct)
        {
            return await _db.UmapProjections
                .AsNoTracking()
                .Where(u => u.ClusterRunId == clusterRunId)
                .Select(u => new UmapPoint(
                    u.Id,
                    u.Embedding.ClusterId ?? -1,
                    u.X,
                    u.Y,
                    u.Embedding.ClusterConfidence ?? 0f,
                    u.Embedding.Idea.ProblemSummary))
                .ToListAsync(ct);
        }

        public async Task<ClusterDetail?> GetClusterDetailAsync(Guid clusterRunId, int clusterId, CancellationToken ct)
        {
            var cluster = await _db.ProblemClusters
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClusterRunId == clusterRunId && c.ClusterId == clusterId, ct);

            if (cluster is null) return null;

            var validations = await _db.ClusterValidations
                .AsNoTracking()
                .Where(v => v.ClusterRunId == clusterRunId)
                .ToListAsync(ct);

            var thisValidation = validations.FirstOrDefault(v => v.ClusterId == clusterId);

            // Clusters that were merged into this one
            var mergedFromIds = validations
                .Where(v => v.Action == ClusterValidationActions.Merge && v.MergeTargetClusterId == clusterId)
                .Select(v => v.ClusterId)
                .ToList();

            var allClusterIds = new[] { clusterId }.Concat(mergedFromIds).ToArray();

            var isCurrentRun = await _db.ThreadSynthesizedIdeasEmbedding
                .AnyAsync(e => e.ClusterRunId == clusterRunId, ct);

            IReadOnlyList<ClusterIdeaSummary> ideas;

            if (isCurrentRun)
            {
                var rows = await _db.ThreadSynthesizedIdeasEmbedding
                    .AsNoTracking()
                    .Where(e => e.ClusterRunId == clusterRunId && e.ClusterId.HasValue && allClusterIds.Contains(e.ClusterId.Value))
                    .OrderByDescending(e => e.ClusterConfidence)
                    .Select(e => new
                    {
                        e.ThreadSynthesizedIdeaId,
                        e.ClusterId,
                        e.ClusterConfidence,
                        e.CreatedAtUtc,
                        e.Idea.ProblemSummary,
                        e.Idea.ProblemDetails,
                        e.Idea.Actor,
                        e.Idea.Industry,
                        e.Idea.CurrentWorkaround,
                        e.Idea.DesiredOutcome,
                        e.Idea.UrgencySignal,
                        e.Idea.SoftwareOpportunity,
                        e.Idea.ActionabilityRationale,
                        e.Idea.SupportingMentionCount,
                        e.Idea.SupportingDistinctAuthorCount,
                    })
                    .ToListAsync(ct);

                ideas = rows.Select(e => new ClusterIdeaSummary(
                    e.ThreadSynthesizedIdeaId,
                    e.ClusterId!.Value,
                    e.ClusterConfidence,
                    JsonSerializer.Serialize(new
                    {
                        problemSummary = e.ProblemSummary,
                        problemDetails = e.ProblemDetails,
                        actor = e.Actor,
                        industry = e.Industry,
                        currentWorkaround = e.CurrentWorkaround,
                        desiredOutcome = e.DesiredOutcome,
                        urgencySignal = e.UrgencySignal,
                        softwareOpportunity = e.SoftwareOpportunity,
                        actionabilityRationale = e.ActionabilityRationale,
                        supportingMentionCount = e.SupportingMentionCount,
                        supportingDistinctAuthorCount = e.SupportingDistinctAuthorCount,
                    }),
                    e.CreatedAtUtc))
                    .ToList();
            }
            else
            {
                ideas = await _db.IdeaEmbeddingArchives
                    .AsNoTracking()
                    .Where(a => a.ClusterRunId == clusterRunId && a.ClusterId.HasValue && allClusterIds.Contains(a.ClusterId.Value))
                    .OrderByDescending(a => a.ClusterConfidence)
                    .Select(a => new ClusterIdeaSummary(
                        a.ThreadSynthesizedIdeaId,
                        a.ClusterId!.Value,
                        a.ClusterConfidence,
                        a.IdeaSnapshot,
                        a.CreatedAtUtc))
                    .ToListAsync(ct);
            }

            int mergedSize = 0;
            if (mergedFromIds.Count > 0)
            {
                var mergedClusters = await _db.ProblemClusters
                    .AsNoTracking()
                    .Where(c => c.ClusterRunId == clusterRunId && mergedFromIds.Contains(c.ClusterId))
                    .ToListAsync(ct);
                mergedSize = mergedClusters.Sum(c => c.Size);
            }

            return new ClusterDetail(
                cluster.ClusterId,
                cluster.Label ?? $"Cluster {cluster.ClusterId}",
                cluster.Description,
                cluster.Opportunity,
                cluster.Size + mergedSize,
                cluster.AvgConfidence,
                ideas,
                thisValidation?.Action.ToString(),
                mergedFromIds.Count > 0 ? mergedFromIds : null);
        }

        public async Task<bool> SaveValidationAsync(Guid clusterRunId, int clusterId, ClusterValidationInput input, CancellationToken ct)
        {
            var cluster = await _db.ProblemClusters
                .FirstOrDefaultAsync(c => c.ClusterRunId == clusterRunId && c.ClusterId == clusterId, ct);

            if (cluster is null) return false;

            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var existing = await _db.ClusterValidations
                .FirstOrDefaultAsync(v => v.ClusterRunId == clusterRunId && v.ClusterId == clusterId, ct);

            if (existing is null)
            {
                _db.ClusterValidations.Add(new ClusterValidationEntity
                {
                    Id = Guid.NewGuid(),
                    ClusterRunId = clusterRunId,
                    ClusterId = clusterId,
                    CoherenceRating = input.CoherenceRating,
                    NoveltyRating = input.NoveltyRating,
                    ProductPotentialRating = input.ProductPotentialRating,
                    Action = input.Action,
                    Notes = input.Notes,
                    MergeTargetClusterId = input.MergeTargetClusterId,
                    ReviewedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                existing.CoherenceRating = input.CoherenceRating;
                existing.NoveltyRating = input.NoveltyRating;
                existing.ProductPotentialRating = input.ProductPotentialRating;
                existing.Action = input.Action;
                existing.Notes = input.Notes;
                existing.MergeTargetClusterId = input.MergeTargetClusterId;
                existing.ReviewedAtUtc = DateTime.UtcNow;
            }

            if (input.Action == ClusterValidationActions.Merge && input.MergeTargetClusterId.HasValue)
            {
                var targetClusterId = input.MergeTargetClusterId.Value;

                var targetCluster = await _db.ProblemClusters
                    .FirstOrDefaultAsync(c => c.ClusterRunId == clusterRunId && c.ClusterId == targetClusterId, ct);

                if (targetCluster is not null)
                {
                    // Move ideas from source cluster to target cluster
                    var isCurrentRun = await _db.ThreadSynthesizedIdeasEmbedding
                        .AnyAsync(e => e.ClusterRunId == clusterRunId, ct);

                    if (isCurrentRun)
                    {
                        await _db.ThreadSynthesizedIdeasEmbedding
                            .Where(e => e.ClusterRunId == clusterRunId && e.ClusterId == clusterId)
                            .ExecuteUpdateAsync(s => s.SetProperty(e => e.ClusterId, targetClusterId), ct);
                    }
                    else
                    {
                        await _db.IdeaEmbeddingArchives
                            .Where(a => a.ClusterRunId == clusterRunId && a.ClusterId == clusterId)
                            .ExecuteUpdateAsync(s => s.SetProperty(a => a.ClusterId, targetClusterId), ct);
                    }

                    // Recalculate target size and weighted average confidence
                    int newSize = targetCluster.Size + cluster.Size;
                    targetCluster.AvgConfidence = newSize > 0
                        ? (targetCluster.AvgConfidence * targetCluster.Size + cluster.AvgConfidence * cluster.Size) / newSize
                        : targetCluster.AvgConfidence;
                    targetCluster.Size = newSize;

                    // Delete the source cluster — its ideas now live under the target
                    _db.ProblemClusters.Remove(cluster);
                }
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return true;
        }

        public async Task<bool> SetPinnedAsync(Guid clusterRunId, bool pinned, CancellationToken cancellationToken)
        {
            var run = await _db.ClusterRuns.FindAsync([clusterRunId], cancellationToken);
            if (run is null) return false;

            run.IsPinned = pinned;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static double MapUrgency(string urgency) => urgency.ToLowerInvariant() switch
        {
            "high" => 1.0,
            "medium" => 0.5,
            _ => 0.0
        };

        private static double Normalize(double value, double max) =>
            max <= 0 ? 0 : Math.Clamp(value / max, 0.0, 1.0);

        private sealed record IdeaScoreInput(
            int ClusterId,
            float? Confidence,
            string UrgencySignal,
            bool SoftwareOpportunity,
            bool HasWorkaround,
            int MentionCount,
            int AuthorCount);

        private sealed class IdeaSnapshotDto
        {
            public string UrgencySignal { get; set; } = "low";
            public bool SoftwareOpportunity { get; set; }
            public string? CurrentWorkaround { get; set; }
            public int SupportingMentionCount { get; set; }
            public int SupportingDistinctAuthorCount { get; set; }
        }
    }
}
