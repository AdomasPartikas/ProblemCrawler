using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using ProblemCrawler.Core.Configuration;
using ProblemCrawler.Collectors.Reddit.Contracts;
using ProblemCrawler.Core.Models;
using ProblemCrawler.Core.Enums;
using ProblemCrawler.Core.Extensions;

namespace ProblemCrawler.Collectors.Reddit.Services;

/// <summary>
/// HTTP client for fetching data from the Reddit API using public endpoints (no authentication required).
/// </summary>
public class RedditHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RedditHttpClient> _logger;
    private readonly RedditCollectorConfiguration _config;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedditHttpClient(
        HttpClient httpClient,
        ILogger<RedditHttpClient> logger,
        RedditCollectorConfiguration config)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));

        // Configure JSON serialization to handle Reddit's naming conventions
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Fetches a page of posts from a subreddit.
    /// </summary>
    /// <param name="subreddit">Subreddit name (without r/ prefix)</param>
    /// <param name="after">Pagination token from previous request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The API response containing posts</returns>
    public async Task<RedditApiResponse?> GetSubredditPostsAsync(
        string subreddit,
        string? after = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subreddit))
            throw new ArgumentException("Subreddit name cannot be empty", nameof(subreddit));

        var url = BuildSubredditUrl(subreddit, after);
        return await FetchWithRetryAsync<RedditApiResponse>(url, cancellationToken);
    }

    /// <summary>
    /// Fetches comments for a specific post.
    /// </summary>
    /// <param name="subreddit">Subreddit name (without r/ prefix)</param>
    /// <param name="postId">Post ID</param>
    /// <param name="after">Pagination token from previous request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An array of API responses. Index 0 is the post, index 1+ are comments</returns>
    public async Task<RedditApiResponse[]?> GetPostCommentsAsync(
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
        return await FetchWithRetryAsync<RedditApiResponse[]>(url, cancellationToken);
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
    private string BuildPostCommentsUrl(string subreddit, string postId, string? after)
    {
        return RedditApiRouteBuilder.BuildPostCommentsUrl(
            _config.BaseUrl,
            subreddit,
            postId,
            after);
    }

    /// <summary>
    /// Fetches data from Reddit with automatic retry logic.
    /// </summary>
    private async Task<T?> FetchWithRetryAsync<T>(string url, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetching from Reddit: {Url}", url);

        for (int attempt = 0; attempt < _config.MaxRetries; attempt++)
        {
            try
            {
                // Add delay between requests to respect rate limits
                if (attempt > 0)
                {
                    await Task.Delay(_config.RequestDelayMs, cancellationToken);
                }

                var response = await _httpClient.GetAsync(url, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var result = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                    _logger.LogDebug("Successfully fetched from Reddit");
                    return result;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds ?? 1;
                    _logger.LogWarning(
                        "Rate limited by Reddit. Waiting {Seconds} seconds before retry",
                        retryAfter);
                    await Task.Delay((int)(retryAfter * 1000), cancellationToken);
                    continue;
                }

                _logger.LogWarning(
                    "Reddit returned {StatusCode} on attempt {Attempt}/{MaxRetries}",
                    response.StatusCode,
                    attempt + 1,
                    _config.MaxRetries);

                if (attempt < _config.MaxRetries - 1)
                {
                    await Task.Delay(_config.RequestDelayMs, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Request was cancelled");
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    ex,
                    "HTTP error on attempt {Attempt}/{MaxRetries}: {Message}",
                    attempt + 1,
                    _config.MaxRetries,
                    ex.Message);

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
        return default(T);
    }
}
