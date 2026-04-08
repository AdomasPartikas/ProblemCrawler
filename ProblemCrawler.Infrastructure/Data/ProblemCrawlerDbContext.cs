using Microsoft.EntityFrameworkCore;
using ProblemCrawler.Infrastructure.Entities;

namespace ProblemCrawler.Infrastructure.Data
{
    /// <summary>
    /// Entity Framework Core DbContext for the ProblemCrawler application.
    /// Provides data access and ORM functionality for managing normalized collector items from various data sources.
    /// </summary>
    /// <remarks>
    /// This context is configured to automatically apply all entity type configurations from the infrastructure assembly.
    /// It serves as the main entry point for database operations and manages the lifecycle of entities within the ProblemCrawler system.
    /// </remarks>
    public class ProblemCrawlerDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<CollectorItemEntity> CollectorItems => Set<CollectorItemEntity>();
        public DbSet<AnalysedItemEntity> AnalysedItems => Set<AnalysedItemEntity>();
        public DbSet<ThreadSynthesisRunEntity> ThreadSynthesisRuns => Set<ThreadSynthesisRunEntity>();
        public DbSet<ThreadSynthesizedIdeaEntity> ThreadSynthesizedIdeas => Set<ThreadSynthesizedIdeaEntity>();
        public DbSet<ThreadSynthesizedIdeaEmbeddingEntity> ThreadSynthesizedIdeasEmbedding => Set<ThreadSynthesizedIdeaEmbeddingEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("vector");
            modelBuilder.Entity<CollectorItemEntity>()
                .HasIndex(e => new { e.SourceId, e.Source })
                .IsUnique();
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProblemCrawlerDbContext).Assembly);
        }

    }
}
