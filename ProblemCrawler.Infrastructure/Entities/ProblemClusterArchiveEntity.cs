using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Infrastructure.Entities
{
    public sealed class ProblemClusterArchiveEntity
    {
        public Guid Id { get; set; }
        public Guid ClusterRunId { get; set; }
        public int ClusterId { get; set; }
        public int Size { get; set; }
        public float AvgConfidence { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        public ClusterRunEntity ClusterRun { get; set; } = null!;
    }
}
