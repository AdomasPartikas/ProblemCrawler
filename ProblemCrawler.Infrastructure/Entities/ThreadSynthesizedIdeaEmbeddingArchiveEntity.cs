using Pgvector;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Infrastructure.Entities
{
    public sealed class ThreadSynthesizedIdeaEmbeddingArchiveEntity
    {
        public Guid Id { get; set; }
        public Guid ClusterRunId { get; set; }
        public Guid ThreadSynthesizedIdeaId { get; set; }
        public string Model { get; set; } = string.Empty;
        public Vector Embedding { get; set; } = null!;
        public int? ClusterId { get; set; }
        public float? ClusterConfidence { get; set; }
        public string IdeaSnapshot { get; set; } = "{}";
        public DateTime CreatedAtUtc { get; set; }

        public ClusterRunEntity ClusterRun { get; set; } = null!;
    }
}
