using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Core.Records.Embedding
{
    public record EmbeddingCandidate(
    Guid IdeaId,
    string ProblemSummary,
    string? ProblemDetails,
    string? Actor,
    string? Industry,
    string? DesiredOutcome,
    string? CurrentWorkaround
    );
}
