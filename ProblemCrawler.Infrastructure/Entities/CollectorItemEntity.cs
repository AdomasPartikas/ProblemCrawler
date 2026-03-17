using ProblemCrawler.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Infrastructure.Entities
{
    public class CollectorItemEntity
    {
        public Guid Id { get; set; }
        public required string SourceId { get; set; }
        public required string ItemType { get; set; }
        public required string Source { get; set; }
        /// <summary>
        /// Sometimes Content contains a link and no contentm, this is for those cases for now.
        /// </summary>
        public string? SelfText { get; set; }
        public string? Content { get; set; }
        public string? ParentId { get; set; }
        public string? LinkId { get; set; }
        public AnalysisStages AnalysisStage { get; set; } = AnalysisStages.New;
        public Dictionary<string, object?> Metadata { get; set; } = [];
        public required DateTime CreatedAt { get; set; }
        public string? Author { get; set; }
        public string? SourceUrl { get; set; }
    }
}
