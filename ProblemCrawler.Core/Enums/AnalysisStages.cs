using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ProblemCrawler.Core.Enums
{
    public enum AnalysisStages
    {
        /// <summary>
        /// New pending to be processed
        /// </summary>
        New,
        /// <summary>
        /// Filtered out
        /// </summary>
        Removed,
        /// <summary>
        /// Deleted due to being removed or corrupted
        /// </summary>
        Deleted,
        /// <summary>
        /// Processed by LLM
        /// </summary>
        LLM,
        /// <summary>
        /// Analysis of data finished 
        /// </summary>
        Finished



    }
}
