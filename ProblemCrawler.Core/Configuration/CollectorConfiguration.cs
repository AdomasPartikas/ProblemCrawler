using ProblemCrawler.Core.Constants;
using ProblemCrawler.Core.Enums;

namespace ProblemCrawler.Core.Configuration;

/// <summary>
/// Configuration options for collectors.
/// </summary>
public interface ICollectorConfiguration
{
    /// <summary>
    /// The delay between requests to avoid rate limiting (in milliseconds)
    /// </summary>
    int RequestDelayMs { get; }

    /// <summary>
    /// Maximum number of retry attempts for failed requests
    /// </summary>
    int MaxRetries { get; }

    /// <summary>
    /// Timeout for individual requests (in milliseconds)
    /// </summary>
    int RequestTimeoutMs { get; }
}

/// <summary>
/// Configuration specific to the Reddit collector
/// </summary>
public class RedditCollectorConfiguration : ICollectorConfiguration
{
    /// <summary>
    /// Reddit API requests are rate-limited. Default is 2 requests per second (500ms between requests)
    /// </summary>
    public int RequestDelayMs { get; set; } = RedditCollectorDefaults.RequestDelayMs;

    /// <summary>
    /// Number of retries for failed requests
    /// </summary>
    public int MaxRetries { get; set; } = RedditCollectorDefaults.MaxRetries;

    /// <summary>
    /// Timeout for Reddit API requests (in milliseconds)
    /// </summary>
    public int RequestTimeoutMs { get; set; } = RedditCollectorDefaults.RequestTimeoutMs;

    /// <summary>
    /// User agent to use when making requests to Reddit
    /// Reddit API requires a descriptive User-Agent
    /// </summary>
    public string UserAgent { get; set; } = RedditCollectorDefaults.UserAgent;

    /// <summary>
    /// Base URL used by the Reddit collector HTTP client.
    /// </summary>
    public string BaseUrl { get; set; } = RedditCollectorDefaults.BaseUrl;

    /// <summary>
    /// Maximum number of listing pages to fetch per subreddit in a single run.
    /// A value of 1 means only the first page is fetched and no "after" pagination is followed.
    /// null or 0 means keep following the "after" token until Reddit has no more pages.
    /// </summary>
    public int? MaxPages { get; set; } = null;

    /// <summary>
    /// Whether to fetch and process comments for each post.
    /// </summary>
    public bool FetchComments { get; set; } = true;

    /// <summary>
    /// Maximum number of comments to fetch per post.
    /// null or 0 means fetch all available comments by following pagination until exhausted.
    /// </summary>
    public int? MaxCommentsPerPost { get; set; } = 0;

    /// <summary>
    /// Sort order for posts ("hot", "new", "top", "rising", "controversial")
    /// </summary>
    public RedditSort Sort { get; set; } = RedditSort.New;

    /// <summary>
    /// Time range for sorting ("all", "day", "week", "month", "year")
    /// Only applicable for "top" and "controversial" sorts
    /// </summary>
    public RedditTimeRange TimeRange { get; set; } = RedditTimeRange.Week;
}
