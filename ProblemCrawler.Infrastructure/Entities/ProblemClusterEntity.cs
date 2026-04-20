using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Infrastructure.Entities
{
    public sealed class ProblemClusterEntity
    {
        public Guid Id { get; set; }
        public Guid ClusterRunId { get; set; }
        public int ClusterId { get; set; }
        public int Size { get; set; }
        public float AvgConfidence { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string? Label { get; set; }
        public string? Description { get; set; }
        public string? Opportunity { get; set; }
        public ClusterRunEntity ClusterRun { get; set; } = null!;

    }
}
