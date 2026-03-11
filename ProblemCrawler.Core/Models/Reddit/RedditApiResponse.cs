namespace ProblemCrawler.Core.Models.Reddit;

public class RedditApiResponse
{
    /// <summary>
    /// The Reddit object kind (typically "Listing")
    /// </summary>
    public string? Kind { get; set; }

    /// <summary>
    /// The data payload containing the actual listing information
    /// </summary>
    public RedditListingData? Data { get; set; }
}

public class RedditListingData
{
    /// <summary>
    /// The "after" value for the next page (pagination token)
    /// Used to fetch the next set of results
    /// </summary>
    public string? After { get; set; }

    /// <summary>
    /// The "before" value for the previous page (pagination token)
    /// </summary>
    public string? Before { get; set; }

    /// <summary>
    /// Total number of items in this response
    /// </summary>
    public int Dist { get; set; }

    /// <summary>
    /// The moderation hash
    /// </summary>
    public string? ModHash { get; set; }

    /// <summary>
    /// Collection of child objects (posts, comments, etc.)
    /// </summary>
    public List<RedditChild> Children { get; set; } = new();
}

public class RedditChild
{
    /// <summary>
    /// The kind of this child object (t1, t2, t3, t5, etc.)
    /// </summary>
    public string? Kind { get; set; }

    /// <summary>
    /// The data of this child object
    /// </summary>
    public RedditChildData? Data { get; set; }
}

/// <summary>
/// Base data class for all Reddit child objects.
/// Contains common fields that appear across different object types.
/// </summary>
public class RedditChildData
{
    /// <summary>
    /// The unique ID of this object (without the kind prefix)
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// The full name of this object (kind + id, e.g., "t3_abc123")
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The subreddit name without the "r/" prefix
    /// </summary>
    public string? Subreddit { get; set; }

    /// <summary>
    /// The timestamp when this object was created (UTC)
    /// </summary>
    public double CreatedUtc { get; set; }

    /// <summary>
    /// The username of the author
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// The full name of the author (includes kind prefix)
    /// </summary>
    public string? AuthorFullname { get; set; }
}
