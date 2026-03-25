using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using ProblemCrawler.Core.Enums;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Core.Models;
using ProblemCrawler.Core.Models.Reddit;
using ProblemCrawler.Core.Records.Filtering;
using ProblemCrawler.Core.Records.LLM;
using ProblemCrawler.Infrastructure.Data;
using ProblemCrawler.Infrastructure.Entities;
using ProblemCrawler.Infrastructure.RawSQL;
using System.Text;
using System.Text.Json;

namespace ProblemCrawler.Infrastructure.Repositories
{
    /// <summary>
    /// Provides methods for managing collector items in the database.
    /// </summary>
    /// <param name="context">The database context used to access and modify collector items. Cannot be null.</param>
    public class CollectorItemRepository(
        ProblemCrawlerDbContext context,
        IMapper mapper
        ) : ICollectorItemRepository
    {
        private readonly ProblemCrawlerDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        /// <summary>
        /// Asynchronously inserts a batch of collector items into the database.
        /// </summary>
        /// <remarks>This method adds all items in the batch and commits them in a single transaction. If
        /// the operation is canceled, no items will be inserted.</remarks>
        /// <param name="items">The list of collector items to be inserted. Cannot be null. Each item will be added to the database in a
        /// single batch operation.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous insert operation.</returns>
        public async Task InsertBatchAsync(List<CollectorItem> items, CancellationToken cancellationToken)
        {
            List<CollectorItemEntity> collectorItemEntities = _mapper.Map<List<CollectorItemEntity>>(items);
            collectorItemEntities = RetrieveMetadata(collectorItemEntities);
            await UpsertBatchAsync(collectorItemEntities, cancellationToken);
        }

        public async Task<IReadOnlyList<CollectorItemFilterCandidate>> GetFilteringCandidatesAsync(
            int batchSize,
            CancellationToken cancellationToken)
        {
            return await _context.CollectorItems
                .AsNoTracking()
                .Where(item =>
                    item.AnalysisStage == AnalysisStages.New)
                .OrderBy(item => item.CreatedAt)
                .Take(batchSize)
                .Select(item => new CollectorItemFilterCandidate(
                    item.Id,
                    item.Content,
                    item.ItemType,
                    item.AnalysisStage))
                .ToListAsync(cancellationToken);
        }

        public async Task UpdateAnalysisStagesAsync(
            IReadOnlyList<CollectorItemFilterUpdate> updates,
            CancellationToken cancellationToken)
        {
            if (updates.Count == 0)
            {
                return;
            }

            var targetStageById = updates.ToDictionary(update => update.Id, update => update.TargetStage);
            var ids = targetStageById.Keys.ToArray();

            var entities = await _context.CollectorItems
                .Where(item => ids.Contains(item.Id))
                .ToListAsync(cancellationToken);

            foreach (var entity in entities)
            {
                if (targetStageById.TryGetValue(entity.Id, out var targetStage))
                {
                    entity.AnalysisStage = targetStage;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<LLMAnalysisCandidate>> GetLlmAnalysisCandidatesAsync(
            int batchSize,
            CancellationToken cancellationToken)
        {
            return await _context.CollectorItems
                .AsNoTracking()
                .Where(item => item.AnalysisStage == AnalysisStages.ReadyForAnalysis)
                .OrderBy(item => item.CreatedAt)
                .Take(batchSize)
                .Select(item => new LLMAnalysisCandidate(
                    item.Id,
                    item.Source,
                    item.SourceId,
                    item.ItemType,
                    item.Content,
                    item.ParentId,
                    item.LinkId,
                    item.AnalysisStage))
                .ToListAsync(cancellationToken);
        }

        public async Task<LLMAnalysisCandidate?> GetLlmAnalysisCandidateByIdAsync(
            Guid collectorItemId,
            CancellationToken cancellationToken)
        {
            return await _context.CollectorItems
                .AsNoTracking()
                .Where(item => item.Id == collectorItemId)
                .Select(item => new LLMAnalysisCandidate(
                    item.Id,
                    item.Source,
                    item.SourceId,
                    item.ItemType,
                    item.Content,
                    item.ParentId,
                    item.LinkId,
                    item.AnalysisStage))
                .SingleOrDefaultAsync(cancellationToken);
        }

        public async Task<LLMAnalysisContext?> GetLlmAnalysisContextAsync(
            Guid collectorItemId,
            CancellationToken cancellationToken)
        {
            var currentEntity = await _context.CollectorItems
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == collectorItemId, cancellationToken);

            if (currentEntity is null)
            {
                return null;
            }

            var current = MapContextItem(currentEntity);
            LLMContextItem? parent = null;
            LLMContextItem? post = null;

            if (string.Equals(currentEntity.ItemType, "Comment", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(currentEntity.LinkId))
                {
                    var postEntity = await _context.CollectorItems
                        .AsNoTracking()
                        .SingleOrDefaultAsync(item =>
                            item.Source == currentEntity.Source &&
                            item.SourceId == currentEntity.LinkId,
                            cancellationToken);

                    if (postEntity is not null)
                    {
                        post = MapContextItem(postEntity);
                    }
                }

                if (!string.IsNullOrWhiteSpace(currentEntity.ParentId) &&
                    !string.Equals(currentEntity.ParentId, currentEntity.LinkId, StringComparison.Ordinal))
                {
                    var parentEntity = await _context.CollectorItems
                        .AsNoTracking()
                        .SingleOrDefaultAsync(item =>
                            item.Source == currentEntity.Source &&
                            item.SourceId == currentEntity.ParentId,
                            cancellationToken);

                    if (parentEntity is not null)
                    {
                        parent = MapContextItem(parentEntity);
                    }
                }
            }

            return new LLMAnalysisContext(current, parent, post);
        }

        public async Task UpsertAnalysedItemAsync(
            AnalysedItemUpsert analysis,
            CancellationToken cancellationToken)
        {
            var item = await _context.CollectorItems
                .SingleOrDefaultAsync(x => x.Id == analysis.CollectorItemId, cancellationToken);

            if (item is null)
            {
                return;
            }

            var existing = await _context.AnalysedItems
                .SingleOrDefaultAsync(x => x.CollectorItemId == analysis.CollectorItemId, cancellationToken);

            if (existing is null)
            {
                existing = new AnalysedItemEntity
                {
                    Id = Guid.NewGuid(),
                    CollectorItemId = analysis.CollectorItemId,
                    UpdatedAtUtc = analysis.AnalyzedAtUtc
                };

                _context.AnalysedItems.Add(existing);
            }

            var rootCollectorItemId = await ResolveRootCollectorItemIdAsync(item, cancellationToken);

            existing.ContainsProblem = analysis.Result.ContainsProblem;
            existing.ProblemSummary = analysis.Result.ProblemSummary;
            existing.ProblemDetails = analysis.Result.ProblemDetails;
            existing.Actor = analysis.Result.Actor;
            existing.Industry = analysis.Result.Industry;
            existing.CurrentWorkaround = analysis.Result.CurrentWorkaround;
            existing.DesiredOutcome = analysis.Result.DesiredOutcome;
            existing.UrgencySignal = analysis.Result.UrgencySignal;
            existing.SoftwareOpportunity = analysis.Result.SoftwareOpportunity;
            existing.IsActionable = analysis.Result.IsActionable;
            existing.ActionabilityRationale = analysis.Result.ActionabilityRationale;
            existing.RawJson = analysis.RawJson;
            existing.Model = analysis.Model;
            existing.AnalyzedAtUtc = analysis.AnalyzedAtUtc;
            existing.UpdatedAtUtc = analysis.AnalyzedAtUtc;
            existing.RootCollectorItemId = rootCollectorItemId;

            item.AnalysisStage = AnalysisStages.Analysed;

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ThreadSynthesisCandidate>> GetThreadSynthesisCandidatesAsync(
            int batchSize,
            CancellationToken cancellationToken)
        {
            var effectiveBatchSize = batchSize <= 0 ? 100 : batchSize;

            var rawJoined = await _context.AnalysedItems
                .AsNoTracking()
                .Where(item =>
                    item.ContainsProblem &&
                    item.SoftwareOpportunity &&
                    item.IsActionable)
                .Join(
                    _context.CollectorItems.AsNoTracking(),
                    analysed => analysed.CollectorItemId,
                    collector => collector.Id,
                    (analysed, collector) => new
                    {
                        analysed.RootCollectorItemId,
                        CollectorItemId = collector.Id,
                        collector.CreatedAt,
                        analysed.UpdatedAtUtc
                    })
                .ToListAsync(cancellationToken);

            var aggregates = rawJoined
                .GroupBy(x => x.RootCollectorItemId)
                .Select(group => new ThreadSynthesisCandidate(
                    group.Key,
                    group.Select(x => x.CollectorItemId).Distinct().Count(),
                    group.Count(),
                    group.Max(x => x.CreatedAt),
                    group.Max(x => x.UpdatedAtUtc)))
                .OrderBy(candidate => candidate.LatestAnalysedItemUpdatedAtUtc)
                .ToList();

            if (aggregates.Count == 0)
            {
                return [];
            }

            var rootCollectorItemIds = aggregates
                .Select(x => x.RootCollectorItemId)
                .Distinct()
                .ToArray();

            var existingRuns = await _context.ThreadSynthesisRuns
                .AsNoTracking()
                .Where(run => rootCollectorItemIds.Contains(run.RootCollectorItemId))
                .OrderByDescending(run => run.AnalyzedAtUtc)
                .ToListAsync(cancellationToken);

            var latestRunByRootCollectorItemId = existingRuns
                .GroupBy(run => run.RootCollectorItemId)
                .ToDictionary(group => group.Key, group => group.First());

            var staleCandidates = aggregates
                .Where(candidate =>
                {
                    if (!latestRunByRootCollectorItemId.TryGetValue(candidate.RootCollectorItemId, out var latestRun))
                    {
                        return true;
                    }

                    return latestRun.ThreadItemCount != candidate.ThreadItemCount ||
                           latestRun.AnalysedItemCount != candidate.AnalysedItemCount ||
                           latestRun.LatestCollectorItemCreatedAtUtc < candidate.LatestCollectorItemCreatedAtUtc ||
                           latestRun.LatestAnalysedItemUpdatedAtUtc < candidate.LatestAnalysedItemUpdatedAtUtc;
                })
                .Take(effectiveBatchSize)
                .ToList();

            return staleCandidates;
        }

        public async Task<ThreadSynthesisContext?> GetThreadSynthesisContextAsync(
            Guid rootCollectorItemId,
            CancellationToken cancellationToken)
        {
            var rootCollectorItem = await _context.CollectorItems
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == rootCollectorItemId, cancellationToken);

            if (rootCollectorItem is null)
            {
                return null;
            }

            var threadItems = await _context.AnalysedItems
                .AsNoTracking()
                .Where(item =>
                    item.RootCollectorItemId == rootCollectorItemId &&
                    item.ContainsProblem &&
                    item.SoftwareOpportunity &&
                    item.IsActionable)
                .Join(
                    _context.CollectorItems.AsNoTracking(),
                    analysed => analysed.CollectorItemId,
                    collector => collector.Id,
                    (analysed, collector) => new { analysed, collector })
                .OrderBy(x => x.collector.CreatedAt)
                .ToListAsync(cancellationToken);

            if (threadItems.Count == 0)
            {
                return null;
            }

            var synthesisItems = threadItems
                .Select(x => new ThreadSynthesisSourceItem(
                    x.analysed.Id,
                    x.collector.Id,
                    x.collector.SourceId,
                    x.collector.ItemType,
                    ResolveTitle(x.collector.Metadata),
                    x.collector.Content,
                    x.collector.ParentId,
                    x.collector.LinkId,
                    x.collector.Author,
                    x.collector.CreatedAt,
                    x.collector.SourceUrl,
                    x.analysed.ContainsProblem,
                    x.analysed.ProblemSummary,
                    x.analysed.ProblemDetails,
                    x.analysed.Actor,
                    x.analysed.Industry,
                    x.analysed.CurrentWorkaround,
                    x.analysed.DesiredOutcome,
                    x.analysed.UrgencySignal,
                    x.analysed.SoftwareOpportunity,
                    x.analysed.IsActionable,
                    x.analysed.ActionabilityRationale,
                    x.analysed.AnalyzedAtUtc,
                    x.analysed.UpdatedAtUtc))
                .ToList();

            return new ThreadSynthesisContext(
                rootCollectorItemId,
                MapContextItem(rootCollectorItem),
                synthesisItems,
                synthesisItems.Count,
                synthesisItems.Count,
                synthesisItems.Max(item => item.CreatedAtUtc),
                synthesisItems.Max(item => item.AnalysedUpdatedAtUtc));
        }

        public async Task UpsertThreadSynthesisAsync(
            ThreadSynthesisUpsert synthesis,
            CancellationToken cancellationToken)
        {
            var run = new ThreadSynthesisRunEntity
            {
                Id = Guid.NewGuid(),
                RootCollectorItemId = synthesis.RootCollectorItemId,
                ThreadItemCount = synthesis.ThreadItemCount,
                AnalysedItemCount = synthesis.AnalysedItemCount,
                LatestCollectorItemCreatedAtUtc = synthesis.LatestCollectorItemCreatedAtUtc,
                LatestAnalysedItemUpdatedAtUtc = synthesis.LatestAnalysedItemUpdatedAtUtc,
                Model = synthesis.Model,
                AnalyzedAtUtc = synthesis.AnalyzedAtUtc,
                UpdatedAtUtc = synthesis.AnalyzedAtUtc
            };

            _context.ThreadSynthesisRuns.Add(run);

            foreach (var idea in synthesis.Ideas)
            {
                _context.ThreadSynthesizedIdeas.Add(new ThreadSynthesizedIdeaEntity
                {
                    Id = Guid.NewGuid(),
                    ThreadSynthesisRunId = run.Id,
                    ProblemSummary = idea.ProblemSummary,
                    ProblemDetails = idea.ProblemDetails,
                    Actor = idea.Actor,
                    Industry = idea.Industry,
                    CurrentWorkaround = idea.CurrentWorkaround,
                    DesiredOutcome = idea.DesiredOutcome,
                    UrgencySignal = idea.UrgencySignal,
                    SoftwareOpportunity = idea.SoftwareOpportunity,
                    IsActionable = idea.IsActionable,
                    ActionabilityRationale = idea.ActionabilityRationale,
                    SupportingMentionCount = idea.SupportingMentionCount,
                    SupportingDistinctAuthorCount = idea.SupportingDistinctAuthorCount,
                    RawJson = idea.RawJson,
                    AnalyzedAtUtc = synthesis.AnalyzedAtUtc,
                    UpdatedAtUtc = synthesis.AnalyzedAtUtc
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        /// <summary>
        /// Inserts or updates a batch of collector item entities in the database asynchronously, ensuring that existing
        /// records are updated if a conflict occurs on SourceId and Source.
        /// </summary>
        /// <remarks>If a conflict occurs on the combination of SourceId and Source, the existing record
        /// is updated with the new values for Content, ParentId, LinkId, Metadata, Author, SourceUrl, and
        /// AnalysisStage. The operation is performed within a database transaction to ensure atomicity.</remarks>
        /// <param name="items">The list of collector item entities to insert or update. Each entity represents a record to be upserted in
        /// the database. Cannot be null.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous upsert operation.</returns>
        public async Task UpsertBatchAsync(
            List<CollectorItemEntity> items,
            CancellationToken cancellationToken)
        {

            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var sqlBuilder = new StringBuilder();
                var parameters = new List<Npgsql.NpgsqlParameter>();

                sqlBuilder.Append(ContentItemSql.insertionSql);

                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];

                    if (i > 0) sqlBuilder.Append(", ");
                    sqlBuilder.Append('(');
                    sqlBuilder.Append($"@p{i}_id,@p{i}_sourceId,@p{i}_source,@p{i}_itemType,");
                    sqlBuilder.Append($"@p{i}_content,@p{i}_parentId,@p{i}_linkId,");
                    sqlBuilder.Append($"@p{i}_metadata,@p{i}_createdAt,@p{i}_author,@p{i}_sourceUrl,@p{i}_analysisStage");
                    sqlBuilder.Append(')');

                    parameters.Add(new Npgsql.NpgsqlParameter($"p{i}_id", item.Id));
                    parameters.Add(new Npgsql.NpgsqlParameter($"p{i}_sourceId", item.SourceId));
                    parameters.Add(new Npgsql.NpgsqlParameter($"p{i}_source", item.Source));
                    parameters.Add(new Npgsql.NpgsqlParameter($"p{i}_itemType", item.ItemType));
                    parameters.Add(new Npgsql.NpgsqlParameter($"p{i}_content", item.Content ?? (object)DBNull.Value));
                    parameters.Add(new Npgsql.NpgsqlParameter($"p{i}_parentId", item.ParentId ?? (object)DBNull.Value));
                    parameters.Add(new Npgsql.NpgsqlParameter($"p{i}_linkId", item.LinkId ?? (object)DBNull.Value));
                    parameters.Add(new Npgsql.NpgsqlParameter
                    {
                        ParameterName = $"p{i}_metadata",
                        Value = JsonSerializer.Serialize(item.Metadata),
                        NpgsqlDbType = NpgsqlDbType.Jsonb
                    });
                    parameters.Add(new Npgsql.NpgsqlParameter($"p{i}_createdAt", item.CreatedAt));
                    parameters.Add(new Npgsql.NpgsqlParameter($"p{i}_author", item.Author ?? (object)DBNull.Value));
                    parameters.Add(new Npgsql.NpgsqlParameter($"p{i}_sourceUrl", item.SourceUrl ?? (object)DBNull.Value));
                    parameters.Add(new Npgsql.NpgsqlParameter($"p{i}_analysisStage", item.AnalysisStage.ToString()));
                }

                sqlBuilder.Append(ContentItemSql.conflictUpdateSql);

                await _context.Database.ExecuteSqlRawAsync(sqlBuilder.ToString(), parameters.ToArray(), cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);

                throw;
            }
        }
        /// <summary>
        /// Populates metadata fields for each item in the provided collection based on their type and associated
        /// metadata.
        /// </summary>
        /// <remarks>For items of type "Comment", it extracts and sets the ParentId and LinkId
        /// properties from the metadata, removing any prefix before the underscore character. The method does not
        /// create new instances; it updates the existing entities in place.</remarks>
        /// <param name="collectorItemEntities">A list of collector item entities whose metadata fields will be updated. Cannot be null.</param>
        /// <returns>A list of collector item entities with updated metadata fields. The same list instance as provided in the
        /// input.</returns>
        public static List<CollectorItemEntity> RetrieveMetadata(List<CollectorItemEntity> collectorItemEntities)
        {
            foreach (var item in collectorItemEntities)
            {
                Dictionary<string, object?> metadata = item.Metadata;

                if (metadata is null)
                {
                    continue;
                }

                if (item.ItemType == "Comment")
                {
                    if (metadata.TryGetValue("ParentId", out object? parent) && parent is string parentId)
                    {
                        item.ParentId = parentId;
                    }

                    if (metadata.TryGetValue("LinkId", out object? link) && link is string linkId)
                    {
                        item.LinkId = linkId;
                    }
                }
            }
            return collectorItemEntities;
        }

        private async Task<Guid> ResolveRootCollectorItemIdAsync(CollectorItemEntity item, CancellationToken cancellationToken)
        {
            var rootSourceId =
                string.Equals(item.ItemType, "Comment", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(item.LinkId)
                    ? item.LinkId
                    : item.SourceId;

            if (string.IsNullOrWhiteSpace(rootSourceId))
            {
                return item.Id;
            }

            var rootId = await _context.CollectorItems
                .AsNoTracking()
                .Where(x => x.Source == item.Source && x.SourceId == rootSourceId)
                .Select(x => (Guid?)x.Id)
                .SingleOrDefaultAsync(cancellationToken);

            return rootId ?? item.Id;
        }

        private static LLMContextItem MapContextItem(CollectorItemEntity entity)
        {
            var title = ResolveTitle(entity.Metadata);

            return new LLMContextItem(
                entity.SourceId,
                entity.ItemType,
                title,
                entity.Content,
                entity.Author,
                entity.CreatedAt,
                entity.SourceUrl);
        }

        private static string? ResolveTitle(Dictionary<string, object?> metadata)
        {
            if (metadata.TryGetValue("Title", out var rawTitle) && rawTitle is string mappedTitle)
            {
                return mappedTitle;
            }

            return null;
        }
    }
}
