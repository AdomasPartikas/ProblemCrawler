using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProblemCrawler.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace ProblemCrawler.Infrastructure.Configurations
{
    public sealed class UmapProjectionConfiguration : IEntityTypeConfiguration<UmapProjectionEntity>
    {
        public void Configure(EntityTypeBuilder<UmapProjectionEntity> Builder)
        {
            Builder.HasIndex(e => new { e.ClusterRunId, e.ThreadSynthesizedIdeaEmbeddingId })
                .IsUnique();

            Builder.HasOne(e => e.ClusterRun)
                .WithMany()
                .HasForeignKey(e => e.ClusterRunId)
                .OnDelete(DeleteBehavior.Cascade); 

            Builder.HasOne(e => e.Embedding)
                .WithMany()
                .HasForeignKey(e => e.ThreadSynthesizedIdeaEmbeddingId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
