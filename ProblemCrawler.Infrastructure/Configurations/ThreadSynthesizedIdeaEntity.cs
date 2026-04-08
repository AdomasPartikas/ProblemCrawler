using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProblemCrawler.Infrastructure.Entities;


namespace ProblemCrawler.Infrastructure.Configurations
{
    public class ThreadSynthesizedIdeaEmbeddingConfiguration : IEntityTypeConfiguration<ThreadSynthesizedIdeaEmbeddingEntity>
    {
        public void Configure(EntityTypeBuilder<ThreadSynthesizedIdeaEmbeddingEntity> builder)
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Embedding)
                .HasColumnType("vector(768)");

            builder.HasIndex(e => new { e.ThreadSynthesizedIdeaId, e.Model })
                .IsUnique();

            builder.HasOne(e => e.Idea)
                .WithOne(i => i.Embedding)
                .HasForeignKey<ThreadSynthesizedIdeaEmbeddingEntity>(e => e.ThreadSynthesizedIdeaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
