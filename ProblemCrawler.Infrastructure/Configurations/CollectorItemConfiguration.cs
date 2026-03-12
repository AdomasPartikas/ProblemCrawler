using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProblemCrawler.Core.Models;
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
    public class CollectorItemConfiguration : IEntityTypeConfiguration<CollectorItem>
    {
        public void Configure(EntityTypeBuilder<CollectorItem> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Source)
                .IsRequired();

            builder.Property(x => x.ItemType)
                .IsRequired();

            builder.Property(x => x.Metadata)
                .HasColumnType("jsonb");

            builder.Property(x => x.CreatedAt)
                .IsRequired();
        }
    }
}
