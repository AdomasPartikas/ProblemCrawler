namespace ProblemCrawler.Core.Interfaces;

/// <summary>
/// Represents a single item collected from a source.
/// Can be a post, comment, issue, or any other unit of content.
/// </summary>
public interface ICollectorItem
{
    /// <summary>
    /// Unique identifier for this item within the source
    /// </summary>
    string Id { get; }

    /// <summary>
    /// The type of item (e.g., "Post", "Comment")
    /// </summary>
    string ItemType { get; }

    /// <summary>
    /// The source/platform (e.g., "Reddit", "Twitter")
    /// </summary>
    string Source { get; }

    /// <summary>
    /// The main content text
    /// </summary>
    string? Content { get; }

    /// <summary>
    /// Additional metadata specific to this item
    /// </summary>
    Dictionary<string, object?> Metadata { get; }

    /// <summary>
    /// When this item was created
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// The author of the item
    /// </summary>
    string? Author { get; }

    /// <summary>
    /// Optional: The URL to view this item on the source platform
    /// </summary>
    string? SourceUrl { get; }
}