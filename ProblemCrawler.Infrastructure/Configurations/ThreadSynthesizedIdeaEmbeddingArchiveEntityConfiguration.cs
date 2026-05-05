using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProblemCrawler.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Infrastructure.Configurations
{
    public sealed class ThreadSynthesizedIdeaEmbeddingArchiveEntityConfiguration
     : IEntityTypeConfiguration<ThreadSynthesizedIdeaEmbeddingArchiveEntity>
    {
        public void Configure(EntityTypeBuilder<ThreadSynthesizedIdeaEmbeddingArchiveEntity> builder)
        {
            builder.ToTable("ThreadSynthesizedIdeaEmbeddingArchive");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.IdeaSnapshot).HasColumnType("jsonb").IsRequired();
            builder.Property(x => x.ClusterRunId).IsRequired();
            builder.HasOne(x => x.ClusterRun)
                .WithMany()
                .HasForeignKey(x => x.ClusterRunId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => x.ClusterRunId);
            builder.HasIndex(x => x.ThreadSynthesizedIdeaId);
        }
    }
}
