using ProblemCrawler.Core.Interfaces;

namespace ProblemCrawler.Core.Models;

/// <summary>
/// A concrete implementation of ICollectorItem for items collected from Reddit
/// </summary>
public class RedditCollectorItem : ICollectorItem
{
    public required string Id { get; set; }
    public required string ItemType { get; set; }
    public string Source => "Reddit";
    public string? Content { get; set; }
    public Dictionary<string, object?> Metadata { get; set; } = new();
    public required DateTime CreatedAt { get; set; }
    public string? Author { get; set; }
    public string? SourceUrl { get; set; }

    /// <summary>
    /// Creates a RedditCollectorItem from a Reddit post
    /// </summary>
    public static RedditCollectorItem FromPost(RedditPost post)
    {
        var content = post.IsSelf
            ? post.Selftext ?? string.Empty
            : post.Url ?? string.Empty;

        return new RedditCollectorItem
        {
            Id = post.Id ?? throw new ArgumentNullException(nameof(post.Id)),
            ItemType = "Post",
            Content = content,
            CreatedAt = UnixTimeStampToDateTime(post.CreatedUtc),
            Author = post.Author,
            SourceUrl = post.Permalink != null
                ? $"https://www.reddit.com{post.Permalink}"
                : null,
            Metadata = new Dictionary<string, object?>
            {
                ["Title"] = post.Title,
                ["Score"] = post.Score,
                ["Ups"] = post.Ups,
                ["Downs"] = post.Downs,
                ["UpvoteRatio"] = post.UpvoteRatio,
                ["NumComments"] = post.NumComments,
                ["Subreddit"] = post.Subreddit,
                ["SubredditSubscribers"] = post.SubredditSubscribers,
                ["IsStickied"] = post.Stickied,
                ["IsPinned"] = post.Pinned,
                ["IsArchived"] = post.Archived,
                ["IsLocked"] = post.Locked,
                ["FlairText"] = post.LinkFlairText,
                ["RawPost"] = post, // Include the full post for reference
            }
        };
    }

    /// <summary>
    /// Creates a RedditCollectorItem from a Reddit comment
    /// </summary>
    public static RedditCollectorItem FromComment(RedditComment comment)
    {
        return new RedditCollectorItem
        {
            Id = comment.Id ?? throw new ArgumentNullException(nameof(comment.Id)),
            ItemType = "Comment",
            Content = comment.Body,
            CreatedAt = UnixTimeStampToDateTime(comment.CreatedUtc),
            Author = comment.Author,
            SourceUrl = comment.Permalink != null
                ? $"https://www.reddit.com{comment.Permalink}"
                : null,
            Metadata = new Dictionary<string, object?>
            {
                ["Score"] = comment.Score,
                ["Ups"] = comment.Ups,
                ["Downs"] = comment.Downs,
                ["ScoreHidden"] = comment.ScoreHidden,
                ["Depth"] = comment.Depth,
                ["IsSubmitter"] = comment.IsSubmitter,
                ["ParentId"] = comment.ParentId,
                ["LinkId"] = comment.LinkId,
                ["Subreddit"] = comment.Subreddit,
                ["IsArchived"] = comment.Archived,
                ["IsLocked"] = comment.Locked,
                ["Distinguished"] = comment.Distinguished,
                ["IsPremium"] = comment.AuthorPremium,
                ["RawComment"] = comment, // Include the full comment for reference
            }
        };
    }

    /// <summary>
    /// Converts a Unix timestamp (seconds since epoch) to a DateTime in UTC
    /// </summary>
    private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp);
        return dateTime;
    }
}
