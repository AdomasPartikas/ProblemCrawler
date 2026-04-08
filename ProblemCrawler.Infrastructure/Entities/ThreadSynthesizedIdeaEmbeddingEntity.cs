using Pgvector;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Infrastructure.Entities
{
    public sealed class ThreadSynthesizedIdeaEmbeddingEntity
    {
        public Guid Id { get; set; }
        public Guid ThreadSynthesizedIdeaId { get; set; }
        public string Model { get; set; } = string.Empty;
        public Vector Embedding { get; set; } = null!;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public ThreadSynthesizedIdeaEntity Idea { get; set; } = null!;
    }
}
