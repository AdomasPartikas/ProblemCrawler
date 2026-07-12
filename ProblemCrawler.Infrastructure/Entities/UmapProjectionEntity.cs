using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Infrastructure.Entities
{
    public sealed class UmapProjectionEntity
    {
        public Guid Id { get; set; }
        public Guid ClusterRunId { get; set; }
        public Guid ThreadSynthesizedIdeaEmbeddingId { get; set; }
        public float X { get; set; }
        public float Y { get; set; }

        public ClusterRunEntity ClusterRun { get; set; } = null!;
        public ThreadSynthesizedIdeaEmbeddingEntity Embedding { get; set; } = null!;
    }
}
