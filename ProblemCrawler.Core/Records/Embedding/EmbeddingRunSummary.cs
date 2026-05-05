using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Core.Records.Embedding
{
    public record EmbeddingRunSummary
    (
        int Evaluated,
        int Embedded,
        int Skipped,
        int Failed
    );
    
}
