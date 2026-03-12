namespace ProblemCrawler.Core.Models.Reddit;

/// <summary>
/// Represents the nested replies listing on a Reddit comment.
/// The Reddit API returns this as either an empty string ("") or a Listing object.
/// </summary>
public class RedditReplies
{
    public string? Kind { get; set; }
    public RedditListingData? Data { get; set; }
}
