using ProblemCrawler.Core.Enums;

namespace ProblemCrawler.Infrastructure.Entities
{
    public class CollectorItemEntity
    {
        /// <summary>
        /// The unique identifier for the collected item. This is a GUID to ensure uniqueness across different collectors and sources.
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// The unique identifier for the source from which the item was collected.
        /// </summary>
        public required string SourceId { get; set; }
        /// <summary>
        /// The type of the collected item.
        /// </summary>
        public required string ItemType { get; set; }
        /// <summary>
        /// The source from which the item was collected.
        /// </summary>
        public required string Source { get; set; }
        /// <summary>
        /// The main content of the collected item. This can be text, a URL, or any other relevant data depending on the source and item type.
        /// </summary>
        public string? Content { get; set; }
        /// <summary>
        /// The identifier of the parent item, if applicable. This is used to establish relationships between items, such as comments belonging to a post.
        /// </summary>
        public string? ParentId { get; set; }
        /// <summary>
        /// The identifier of the link associated with the item, if applicable.
        /// </summary>
        public string? LinkId { get; set; }
        /// <summary>
        /// The stage of analysis for the collected item. This is an enum that indicates the current processing state of the item.
        /// </summary>
        public AnalysisStages AnalysisStage { get; set; } = AnalysisStages.New;
        /// <summary>
        /// A dictionary to hold any additional metadata related to the collected item.
        /// </summary>
        public Dictionary<string, object?> Metadata { get; set; } = [];
        /// <summary>
        /// The timestamp when the item was created in the source system. This is important for tracking the age of the item and for any time-based analysis.
        /// </summary>
        public required DateTime CreatedAt { get; set; }
        /// <summary>
        /// The Author of the item, if applicable. This is useful for identifying the source of the content and for any user-based analysis.
        /// </summary>
        public string? Author { get; set; }
        /// <summary>
        /// The URL of the source from which the item was collected, if applicable.
        /// </summary>
        public string? SourceUrl { get; set; }
    }
}
