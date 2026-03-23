using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProblemCrawler.Infrastructure.Entities;

namespace ProblemCrawler.Infrastructure.Configurations;

public sealed class ThreadSynthesizedIdeaConfiguration : IEntityTypeConfiguration<ThreadSynthesizedIdeaEntity>
{
    public void Configure(EntityTypeBuilder<ThreadSynthesizedIdeaEntity> builder)
    {
        builder.ToTable("ThreadSynthesizedIdeas");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProblemSummary)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(x => x.Industry)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.UrgencySignal)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.RawJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(x => x.ThreadSynthesisRunId);
        builder.HasIndex(x => x.Industry);
        builder.HasIndex(x => x.IsActionable);
        builder.HasIndex(x => x.AnalyzedAtUtc);
    }
}