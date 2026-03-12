using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProblemCrawler.Core.Configuration;
using ProblemCrawler.Collectors.Reddit.Contracts;
using ProblemCrawler.Collectors.Reddit.Serialization;
using ProblemCrawler.Collectors.Reddit.Records;
using ProblemCrawler.Core.Enums;
using ProblemCrawler.Core.Models.Reddit;
using ProblemCrawler.Core.Extensions;

namespace ProblemCrawler.Collectors.Reddit.Services;

/// <summary>
/// HTTP client for fetching data from the Reddit API using public endpoints (no authentication required).
/// </summary>
public class RedditHttpClient(
    HttpClient httpClient,
    ILogger<RedditHttpClient> logger,
    RedditCollectorConfiguration config)
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly ILogger<RedditHttpClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly RedditCollectorConfiguration _config = config ?? throw new ArgumentNullException(nameof(config));
    private readonly JsonSerializerOptions _jsonOptions = RedditJsonSerializerOptionsFactory.Create();

    /// <summary>
    /// Fetches a page of posts from a subreddit.
    /// </summary>
    /// <param name="subreddit">Subreddit name (without r/ prefix)</param>
    /// <param name="after">Pagination token from previous request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A parsed page of posts and pagination cursor</returns>
    public async Task<RedditPostsPage> GetSubredditPostsAsync(
        string subreddit,
        string? after = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subreddit))
            throw new ArgumentException("Subreddit name cannot be empty", nameof(subreddit));

        var url = BuildSubredditUrl(subreddit, after);
        var response = await FetchWithRetryAsync<RedditApiResponse>(url, cancellationToken);
        return BuildPostsPage(response);
    }

    /// <summary>
    /// Fetches comments for a specific post.
    /// </summary>
    /// <param name="subreddit">Subreddit name (without r/ prefix)</param>
    /// <param name="postId">Post ID</param>
    /// <param name="after">Pagination token from previous request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A parsed page of comments and pagination cursor</returns>
    public async Task<RedditCommentsPage> GetPostCommentsAsync(
        string subreddit,
        string postId,
        string? after = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subreddit))
            throw new ArgumentException("Subreddit name cannot be empty", nameof(subreddit));

        if (string.IsNullOrWhiteSpace(postId))
            throw new ArgumentException("Post ID cannot be empty", nameof(postId));

        var url = BuildPostCommentsUrl(subreddit, postId, after);
        var response = await FetchWithRetryAsync<RedditApiResponse[]>(url, cancellationToken);
        return BuildCommentsPage(response);
    }

    /// <summary>
    /// Builds the URL for fetching posts from a subreddit.
    /// </summary>
    private string BuildSubredditUrl(string subreddit, string? after)
    {
        var sort = _config.Sort.ToApiValue();
        var timeRange = _config.Sort is RedditSort.Top or RedditSort.Controversial
            ? _config.TimeRange.ToApiValue()
            : null;

        return RedditApiRouteBuilder.BuildSubredditUrl(
            _config.BaseUrl,
            subreddit,
            sort,
            timeRange,
            after);
    }

    /// <summary>
    /// Builds the URL for fetching comments on a post.
    /// </summary>
    private string BuildPostCommentsUrl(string subreddit, string postId, string? after) =>
            RedditApiRouteBuilder.BuildPostCommentsUrl(
                _config.BaseUrl,
                subreddit,
                postId,
                after);

    /// <summary>
    /// Fetches and deserializes data from Reddit with automatic retry logic.
    /// </summary>
    private async Task<T?> FetchWithRetryAsync<T>(string url, CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < _config.MaxRetries; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    await Task.Delay(_config.RequestDelayMs, cancellationToken);
                }

                var response = await _httpClient.GetAsync(url, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var result = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                    return result;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds ?? 1;
                    await Task.Delay((int)(retryAfter * 1000), cancellationToken);
                    continue;
                }

                if (attempt < _config.MaxRetries - 1)
                {
                    await Task.Delay(_config.RequestDelayMs, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching from Reddit");
                throw;
            }
        }

        _logger.LogError("Failed to fetch from Reddit after {MaxRetries} attempts", _config.MaxRetries);
        return default;
    }

    private static RedditPostsPage BuildPostsPage(RedditApiResponse? response)
    {
        if (response?.Data?.Children is null || response.Data.Children.Count == 0)
        {
            return RedditPostsPage.Empty;
        }

        var posts = response.Data.Children
            .Select(static child => child.Data as RedditPost)
            .Where(static post => post is not null)
            .Select(static post => post!)
            .ToList();

        return posts.Count == 0
            ? RedditPostsPage.Empty
            : new RedditPostsPage(response.Data.After, posts);
    }

    private static RedditCommentsPage BuildCommentsPage(RedditApiResponse[]? responses)
    {
        var commentsListing = GetCommentsListing(responses);
        if (commentsListing?.Data?.Children is null || commentsListing.Data.Children.Count == 0)
        {
            return RedditCommentsPage.Empty;
        }

        var comments = commentsListing.Data.Children
            .Select(static child => child.Data as RedditComment)
            .Where(static comment => comment is not null)
            .Select(static comment => comment!)
            .ToList();

        return comments.Count == 0
            ? RedditCommentsPage.Empty
            : new RedditCommentsPage(commentsListing.Data.After, comments);
    }

    private static RedditApiResponse? GetCommentsListing(RedditApiResponse[]? responses) =>
         responses is not null && responses.Length >= 2
            ? responses[1]
            : null;
}
