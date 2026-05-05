using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Core.Configuration
{
    /// <summary>
    /// Settings for the idea embedding pipeline.
    /// </summary>
    public sealed class EmbeddingConfiguration
    {
        /// <summary>
        /// Number of ideas to embed per batch.
        /// </summary>
        public int BatchSize { get; set; } = 25;
    }
}
