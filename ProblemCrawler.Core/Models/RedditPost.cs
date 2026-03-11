namespace ProblemCrawler.Core.Models;

/// <summary>
/// Represents a Reddit post/submission (t3 object type).
/// </summary>
public class RedditPost : RedditChildData
{
    /// <summary>
    /// The title of the post
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The self text (body) of the post (only for self posts)
    /// </summary>
    public string? Selftext { get; set; }

    /// <summary>
    /// HTML version of the selftext
    /// </summary>
    public string? SelftextHtml { get; set; }

    /// <summary>
    /// Whether this is a self post (text post) vs a link post
    /// </summary>
    public bool IsSelf { get; set; }

    /// <summary>
    /// The URL of the post (for link posts)
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// The domain of the URL (e.g., "reddit.com", "example.com")
    /// </summary>
    public string? Domain { get; set; }

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
    /// The ratio of upvotes to total votes
    /// </summary>
    public double UpvoteRatio { get; set; }

    /// <summary>
    /// The number of comments on this post
    /// </summary>
    public int NumComments { get; set; }

    /// <summary>
    /// The permalink URL path to this post
    /// </summary>
    public string? Permalink { get; set; }

    /// <summary>
    /// Whether this post is marked as NSFW
    /// </summary>
    public bool Over18 { get; set; }

    /// <summary>
    /// Whether this post is locked
    /// </summary>
    public bool Locked { get; set; }

    /// <summary>
    /// Whether this post is archived
    /// </summary>
    public bool Archived { get; set; }

    /// <summary>
    /// Whether this post is removed by a moderator
    /// </summary>
    public bool RemovedByCategory { get; set; }

    /// <summary>
    /// The timestamp when this post was edited (0 or false if not edited)
    /// </summary>
    public object? Edited { get; set; }

    /// <summary>
    /// The full name of the subreddit with prefix
    /// </summary>
    public string? SubredditNamePrefixed { get; set; }

    /// <summary>
    /// The ID of the subreddit
    /// </summary>
    public string? SubredditId { get; set; }

    /// <summary>
    /// Number of subscribers to the subreddit
    /// </summary>
    public int SubredditSubscribers { get; set; }

    /// <summary>
    /// Whether this post is stickied
    /// </summary>
    public bool Stickied { get; set; }

    /// <summary>
    /// Whether the post is pinned
    /// </summary>
    public bool Pinned { get; set; }

    /// <summary>
    /// Whether comments are allowed
    /// </summary>
    public bool AllowLiveComments { get; set; }

    /// <summary>
    /// The flair text for this post
    /// </summary>
    public string? LinkFlairText { get; set; }
}
