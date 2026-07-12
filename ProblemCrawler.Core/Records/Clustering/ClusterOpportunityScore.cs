using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Core.Records.Clustering
{
    public record ClusterOpportunityScore(
        int ClusterId,
        string Label,
        string? Description,
        string? Opportunity,
        int Size,
        float AvgConfidence,
        double PainIntensityScore,
        double MentionFrequencyScore,
        double SolutionVacuumScore,
        double SoftwareMarketScore,
        double AuthorBreadthScore,
        double OpportunityScore,
        string? ValidationAction
    );
}
