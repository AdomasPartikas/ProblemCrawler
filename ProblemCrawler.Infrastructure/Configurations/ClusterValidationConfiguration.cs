using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProblemCrawler.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Infrastructure.Configurations
{
    public sealed class ClusterValidationConfiguration : IEntityTypeConfiguration<ClusterValidationEntity>
    {
        public void Configure(EntityTypeBuilder<ClusterValidationEntity> builder)
        {
            builder.ToTable("ClusterValidation");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => new { x.ClusterRunId, x.ClusterId }).IsUnique();
            builder.HasOne(x => x.ClusterRun)
                .WithMany()
                .HasForeignKey(x => x.ClusterRunId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Property(x => x.Action)
                .HasConversion<string>();
        }
    }
}
