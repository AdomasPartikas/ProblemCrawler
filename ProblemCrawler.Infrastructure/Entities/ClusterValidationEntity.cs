using ProblemCrawler.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Infrastructure.Entities
{
    public sealed class ClusterValidationEntity
    {
        public Guid Id { get; set; }
        public Guid ClusterRunId { get; set; }
        public int ClusterId { get; set; }
        public int CoherenceRating { get; set; }
        public int NoveltyRating { get; set; }
        public int ProductPotentialRating { get; set; }
        public ClusterValidationActions Action { get; set; } = ClusterValidationActions.Keep;
        public string? Notes { get; set; }
        public int? MergeTargetClusterId { get; set; }
        public DateTime ReviewedAtUtc { get; set; }

        public ClusterRunEntity ClusterRun { get; set; } = null!;
    }
}
