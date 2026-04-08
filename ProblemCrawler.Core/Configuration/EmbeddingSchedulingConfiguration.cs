using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Core.Configuration
{
    public sealed class EmbeddingSchedulingConfiguration
    {
        /// <summary>
        /// Enables recurring scheduling of the embedding stage.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Cron expression used for the recurring embedding job.
        /// </summary>
        public string CronExpression { get; set; } = "30 * * * *";

        /// <summary>
        /// Time zone identifier used when evaluating the cron expression.
        /// </summary>
        public string TimeZoneId { get; set; } = "UTC";

        /// <summary>
        /// Queues one embedding run when the application starts.
        /// </summary>
        public bool RunOnStartup { get; set; }

        /// <summary>
        /// Allows overlapping runs when the schedule fires before the previous run finishes.
        /// </summary>
        public bool AllowConcurrentRuns { get; set; }
    }
}

