using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Infrastructure.Entities
{
    public sealed class ClusterRunEntity
    {
        public Guid Id { get; set; }
        public string Algorithm { get; set; } = "hdbscan";
        public int MinClusterSize { get; set; }
        public int? MinSamples { get; set; }
        public bool IsPinned { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public ICollection<ProblemClusterEntity> ProblemClusters { get; set; } = [];
    }
}
