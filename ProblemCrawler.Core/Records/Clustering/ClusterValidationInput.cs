using ProblemCrawler.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Core.Records.Clustering
{
    public record ClusterValidationInput(
        int CoherenceRating,
        int NoveltyRating,
        int ProductPotentialRating,
        ClusterValidationActions Action,
        string? Notes,
        int? MergeTargetClusterId);
}
