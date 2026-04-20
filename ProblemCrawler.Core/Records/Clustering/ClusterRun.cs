using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Core.Records.Clustering
{
    public record ClusterRun(
        Guid Id,
        string Algorithm,
        int MinClusterSize,
        int? MinSamples,
        bool IsPinned,
        int TotalClusters,
        int TotalIdeas,
        DateTime CreatedAtUtc);
    }
