using Microsoft.EntityFrameworkCore;
using ProblemCrawler.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

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
    public class ProblemCrawlerDbContext : DbContext
    {
        public ProblemCrawlerDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<CollectorItem> CollectorItems => Set<CollectorItem>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProblemCrawlerDbContext).Assembly);
        }

    }
}
