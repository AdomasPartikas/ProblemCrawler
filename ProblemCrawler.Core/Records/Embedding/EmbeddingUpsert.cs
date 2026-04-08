using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Core.Records.Embedding
{
    public record EmbeddingUpsert
    (
        Guid IdeaId,
        string Model,
        float[] Embedding,
        DateTime CreatedAtUtc
    );
}
