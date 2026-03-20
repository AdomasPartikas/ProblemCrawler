using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProblemCrawler.Infrastructure.Entities;

namespace ProblemCrawler.Infrastructure.Configurations;

public sealed class AnalysedItemConfiguration : IEntityTypeConfiguration<AnalysedItemEntity>
{
    public void Configure(EntityTypeBuilder<AnalysedItemEntity> builder)
    {
        builder.ToTable("AnalysedItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Industry)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.FrequencySignal)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.RawJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.Model)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Confidence)
            .HasPrecision(5, 4);

        builder.HasIndex(x => x.CollectorItemId)
            .IsUnique();

        builder.HasIndex(x => x.IsActionable);
        builder.HasIndex(x => x.Industry);
        builder.HasIndex(x => x.AnalyzedAtUtc);

        builder.HasOne(x => x.CollectorItem)
            .WithOne(x => x.AnalysedItem)
            .HasForeignKey<AnalysedItemEntity>(x => x.CollectorItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
