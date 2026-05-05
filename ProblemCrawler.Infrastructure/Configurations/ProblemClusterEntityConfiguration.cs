using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProblemCrawler.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Infrastructure.Configurations
{
    public sealed class ProblemClusterEntityConfiguration : IEntityTypeConfiguration<ProblemClusterEntity>
    {
        public void Configure(EntityTypeBuilder<ProblemClusterEntity> builder)
        {
            builder.ToTable("ProblemCluster");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ClusterRunId).IsRequired();
        }
    }
}
