namespace ProblemCrawler.Core.Models.Reddit;

/// <summary>
/// Represents a Reddit comment (t1 object type).
/// </summary>
public class RedditComment : RedditChildData
{
    /// <summary>
    /// The text content of the comment
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// HTML version of the comment body
    /// </summary>
    public string? BodyHtml { get; set; }

    /// <summary>
    /// The number of upvotes
    /// </summary>
    public int Ups { get; set; }

    /// <summary>
    /// The number of downvotes
    /// </summary>
    public int Downs { get; set; }

    /// <summary>
    /// The net score (upvotes - downvotes)
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Whether the score is hidden
    /// </summary>
    public bool ScoreHidden { get; set; }

    /// <summary>
    /// The ID of the parent object (post or comment)
    /// Format: "t3_postid" or "t1_commentid"
    /// </summary>
    public string? ParentId { get; set; }

    /// <summary>
    /// The ID of the linked post (used for quick reference)
    /// </summary>
    public string? LinkId { get; set; }

    /// <summary>
    /// Whether this comment is a top-level comment (depth 0)
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// The permalink URL path to this comment
    /// </summary>
    public string? Permalink { get; set; }

    /// <summary>
    /// Whether this is a comment by the original poster
    /// </summary>
    public bool IsSubmitter { get; set; }

    /// <summary>
    /// Whether the comment is locked
    /// </summary>
    public bool Locked { get; set; }

    /// <summary>
    /// Whether the comment is archived
    /// </summary>
    public bool Archived { get; set; }

    /// <summary>
    /// Whether the comment is collapsed
    /// </summary>
    public bool Collapsed { get; set; }

    /// <summary>
    /// The timestamp when this comment was edited (0 or false if not edited)
    /// </summary>
    public object? Edited { get; set; }

    /// <summary>
    /// Whether the comment is removed by a moderator
    /// </summary>
    public string? RemovedByCategory { get; set; }

    /// <summary>
    /// The full name of the subreddit with prefix
    /// </summary>
    public string? SubredditNamePrefixed { get; set; }

    /// <summary>
    /// The ID of the subreddit
    /// </summary>
    public string? SubredditId { get; set; }

    /// <summary>
    /// Replies nested under this comment (not populated by API by default)
    /// </summary>
    public object? Replies { get; set; }

    /// <summary>
    /// Indicates if the comment is distinguishable (e.g., "moderator", "admin")
    /// </summary>
    public string? Distinguished { get; set; }

    /// <summary>
    /// Whether the author is a premium user
    /// </summary>
    public bool AuthorPremium { get; set; }

    /// <summary>
    /// Whether the comment violates community standards
    /// </summary>
    public bool Controversiality { get; set; }
}
