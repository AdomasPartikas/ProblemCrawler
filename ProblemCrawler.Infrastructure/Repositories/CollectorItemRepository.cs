using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using ProblemCrawler.Core.Enums;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Core.Models;
using ProblemCrawler.Core.Models.Reddit;
using ProblemCrawler.Core.Records.Filtering;
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
                    item.AnalysisStage == AnalysisStages.New ||
                    item.AnalysisStage == AnalysisStages.ReadyForAnalysis)
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
    }
}
