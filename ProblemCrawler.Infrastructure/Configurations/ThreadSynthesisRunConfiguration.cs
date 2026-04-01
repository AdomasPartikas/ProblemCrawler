using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProblemCrawler.Infrastructure.Entities;

namespace ProblemCrawler.Infrastructure.Configurations;

public sealed class ThreadSynthesisRunConfiguration : IEntityTypeConfiguration<ThreadSynthesisRunEntity>
{
    public void Configure(EntityTypeBuilder<ThreadSynthesisRunEntity> builder)
    {
        builder.ToTable("ThreadSynthesisRuns");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Model)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(x => x.RootCollectorItemId);
        builder.HasIndex(x => x.AnalyzedAtUtc);

        builder.HasMany(x => x.Ideas)
            .WithOne(x => x.ThreadSynthesisRun)
            .HasForeignKey(x => x.ThreadSynthesisRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<CollectorItemEntity>()
            .WithMany()
            .HasForeignKey(x => x.RootCollectorItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}