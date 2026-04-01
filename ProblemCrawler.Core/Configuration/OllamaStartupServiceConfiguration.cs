using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Core.Configuration
{
    public sealed class OllamaStartupServiceConfiguration
    {
        /// <summary>
        /// Specifies the maximum number of retry attempts allowed for an operation.
        /// </summary>
        public const int MaxRetries = 3;
        /// <summary>
        /// Retry attempt delay 
        /// </summary>
        public static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);

    }
}
