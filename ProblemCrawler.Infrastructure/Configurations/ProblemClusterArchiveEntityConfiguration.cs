using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProblemCrawler.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Infrastructure.Configurations
{
    public sealed class ProblemClusterArchiveEntityConfiguration : IEntityTypeConfiguration<ProblemClusterArchiveEntity>
    {
        public void Configure(EntityTypeBuilder<ProblemClusterArchiveEntity> builder)
        {
            builder.ToTable("ProblemClusterArchive");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ClusterRunId).IsRequired();
            builder.HasOne(x => x.ClusterRun)
                .WithMany()
                .HasForeignKey(x => x.ClusterRunId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => x.ClusterRunId);
        }
    }
}
