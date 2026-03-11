using Microsoft.Extensions.Logging;
using ProblemCrawler.Core.Configuration;
using ProblemCrawler.Core.Enums;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Core.Models;
using ProblemCrawler.Collectors.Reddit.Services;
using ProblemCrawler.Core.Constants;

namespace ProblemCrawler.Collectors.Reddit;

/// <summary>
/// Collector for gathering posts and comments from Reddit subreddits.
/// Implements ICollector to fit into the generic collection pipeline.
/// </summary>
public class RedditCollector(
    RedditHttpClient httpClient,
    ILogger<RedditCollector> logger,
    RedditCollectorConfiguration config) : ICollector
{
    public string Name => "Reddit";

    private readonly RedditHttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly ILogger<RedditCollector> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly RedditCollectorConfiguration _config = config ?? throw new ArgumentNullException(nameof(config));

    /// <summary>
    /// Gathers posts and comments from configured subreddits.
    /// Streams items as they are fetched, allowing for incremental processing.
    /// </summary>
    public async IAsyncEnumerable<ICollectorItem> GatherAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var subreddits = RedditSubredditCatalog.All
            .Where(static subreddit => !string.IsNullOrWhiteSpace(subreddit))
            .ToList();

        if (subreddits.Count == 0)
        {
            _logger.LogWarning("No subreddits configured in the Core subreddit catalog");
            yield break;
        }

        _logger.LogInformation("Starting Reddit collection from subreddits: {Subreddits}",
            string.Join(", ", subreddits));

        foreach (var subreddit in subreddits)
        {
            await foreach (var item in GatherFromSubredditAsync(subreddit, cancellationToken))
            {
                yield return item;
            }
        }

        _logger.LogInformation("Completed Reddit collection");
    }

    /// <summary>
    /// Gathers posts from a single subreddit and optionally their comments.
    /// </summary>
    private async IAsyncEnumerable<ICollectorItem> GatherFromSubredditAsync(
        string subreddit,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogInformation("Gathering posts from r/{Subreddit}", subreddit);

        string? after = null;
        int pageCount = 0;

        while (true)
        {
            if (_config.MaxPages.HasValue && _config.MaxPages > 0 && pageCount >= _config.MaxPages)
            {
                _logger.LogInformation("Reached maximum page limit ({MaxPages}) for r/{Subreddit}",
                    _config.MaxPages, subreddit);
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogDebug("Fetching page {PageNumber} from r/{Subreddit} (after={After})",
                pageCount + 1, subreddit, after ?? "null");

            var response = await _httpClient.GetSubredditPostsAsync(subreddit, after, cancellationToken);

            if (response?.Data?.Children == null || response.Data.Children.Count == 0)
            {
                _logger.LogInformation("No more posts available from r/{Subreddit}", subreddit);
                break;
            }

            // Process posts from this page
            foreach (var child in response.Data.Children)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Only process posts (t3), skip other types
                if (!child.Kind.IsApiKind(RedditObjectKind.Post) || child.Data is not RedditPost post)
                {
                    continue;
                }

                // Yield the post itself
                var postItem = RedditCollectorItem.FromPost(post);
                _logger.LogDebug("Collected post: {PostId} - {Title}", post.Id, post.Title);
                yield return postItem;

                // Fetch and yield comments if configured
                if (_config.FetchComments && post.NumComments > 0)
                {
                    await foreach (var commentItem in GatherCommentsForPostAsync(subreddit, post, cancellationToken))
                    {
                        yield return commentItem;
                    }
                }

                // Respect rate limits between items
                await Task.Delay(_config.RequestDelayMs, cancellationToken);
            }

            // Check for next page
            after = response.Data.After;
            if (string.IsNullOrEmpty(after))
            {
                _logger.LogInformation("No more pages available for r/{Subreddit}", subreddit);
                break;
            }

            pageCount++;
            _logger.LogDebug("Moving to next page for r/{Subreddit}", subreddit);
            await Task.Delay(_config.RequestDelayMs, cancellationToken);
        }
    }

    /// <summary>
    /// Gathers comments for a specific post.
    /// </summary>
    private async IAsyncEnumerable<ICollectorItem> GatherCommentsForPostAsync(
        string subreddit,
        RedditPost post,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogDebug("Gathering comments for post {PostId} from r/{Subreddit}", post.Id, subreddit);

        string? after = null;
        int commentCount = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_config.MaxCommentsPerPost.HasValue &&
                _config.MaxCommentsPerPost > 0 &&
                commentCount >= _config.MaxCommentsPerPost)
            {
                _logger.LogDebug(
                    "Reached maximum comment limit ({MaxComments}) for post {PostId}",
                    _config.MaxCommentsPerPost,
                    post.Id);
                break;
            }

            var response = await _httpClient.GetPostCommentsAsync(subreddit, post.Id!, after, cancellationToken);

            if (response == null || response.Length < 2)
            {
                _logger.LogDebug("No more comments available for post {PostId}", post.Id);
                break;
            }

            // Index 0 is the post, everything after index 0 are comments
            var commentsListing = response[1];
            if (commentsListing?.Data?.Children == null || commentsListing.Data.Children.Count == 0)
            {
                _logger.LogDebug("No comments in this batch for post {PostId}", post.Id);
                break;
            }

            foreach (var child in commentsListing.Data.Children)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Only process comments (t1)
                if (!child.Kind.IsApiKind(RedditObjectKind.Comment) || child.Data is not RedditComment comment)
                {
                    continue;
                }

                var commentItem = RedditCollectorItem.FromComment(comment);
                _logger.LogDebug("Collected comment: {CommentId} by {Author}", comment.Id, comment.Author);
                yield return commentItem;

                commentCount++;

                if (_config.MaxCommentsPerPost.HasValue &&
                    _config.MaxCommentsPerPost > 0 &&
                    commentCount >= _config.MaxCommentsPerPost)
                {
                    break;
                }
            }

            // Check for next page of comments
            after = commentsListing.Data.After;
            if (string.IsNullOrEmpty(after))
            {
                _logger.LogDebug("No more comment pages available for post {PostId}", post.Id);
                break;
            }

            await Task.Delay(_config.RequestDelayMs, cancellationToken);
        }

        _logger.LogDebug("Completed gathering comments for post {PostId}", post.Id);
    }
}
