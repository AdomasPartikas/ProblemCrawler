using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProblemCrawler.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Infrastructure.Configurations
{
    public sealed class ClusterRunEntityConfiguration : IEntityTypeConfiguration<ClusterRunEntity>
    {
        public void Configure( EntityTypeBuilder<ClusterRunEntity> builder)
        {
            builder.ToTable("ClusterRun");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.IsPinned).HasDefaultValue(false);
            builder.HasMany(x => x.ProblemClusters)
                .WithOne(x => x.ClusterRun)
                .HasForeignKey(x => x.ClusterRunId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
