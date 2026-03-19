using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProblemCrawler.Core.Models;
using ProblemCrawler.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Infrastructure.Configurations
{
    /// <summary>
    /// Provides configuration for the CollectorItem entity type within the Entity Framework Core model.
    /// </summary>
    /// <remarks>Defines entity property requirements and mappings, including key selection and column types.
    /// Use this class when configuring the CollectorItem entity in a DbContext to ensure correct schema and validation
    /// rules.</remarks>
    public class CollectorItemConfiguration : IEntityTypeConfiguration<CollectorItemEntity>
    {
        public void Configure(EntityTypeBuilder<CollectorItemEntity> builder)
        {
            builder.Property(x => x.Id)
                .IsRequired();
            builder.Property(x => x.SourceId)
                .IsRequired();

            builder.Property(x => x.Source)
                .IsRequired();

            builder.Property(x => x.Content);

            builder.Property(x => x.ItemType)
                .IsRequired();

            builder.Property(x => x.Metadata)
                .HasColumnType("jsonb");

            builder.Property(x => x.AnalysisStage)
                .HasConversion<string>();

            builder.Property(x => x.CreatedAt)
                .IsRequired();
        }
    }
}
