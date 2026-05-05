using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Core.Configuration
{
    public sealed class ClusteringSchedulingConfiguration
    {
        public bool Enabled { get; set; } 
        public string CronExpression { get; set; } = "0 2 * * *";
        public string TimeZoneId { get; set; } = "UTC";
        public bool RunOnStartup { get; set; } = false;
    }
}
