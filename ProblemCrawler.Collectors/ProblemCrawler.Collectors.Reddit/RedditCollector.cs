using AutoMapper;
using Microsoft.Extensions.Logging;
using ProblemCrawler.Core.Configuration;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Core.Models;
using ProblemCrawler.Core.Models.Reddit;
using ProblemCrawler.Collectors.Reddit.Services;
using ProblemCrawler.Collectors.Reddit.Records;
using ProblemCrawler.Core.Constants;

namespace ProblemCrawler.Collectors.Reddit;

/// <summary>
/// Collector for gathering posts and comments from Reddit subreddits.
/// Implements ICollector to fit into the generic collection pipeline.
/// </summary>
public class RedditCollector(
    RedditHttpClient httpClient,
    ILogger<RedditCollector> logger,
    RedditCollectorConfiguration config,
    IMapper mapper) : ICollector
{
    public string Name => "Reddit";

    private readonly RedditHttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly ILogger<RedditCollector> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly RedditCollectorConfiguration _config = config ?? throw new ArgumentNullException(nameof(config));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    /// <summary>
    /// Gathers posts and comments from configured subreddits.
    /// Streams items as they are fetched, allowing for incremental processing.
    /// </summary>
    public async IAsyncEnumerable<ICollectorItem> GatherAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var subreddits = GetConfiguredSubreddits();

        if (HasNoSubreddits(subreddits))
        {
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

        string? afterToken = null;
        int pageCount = 0;

        while (true)
        {
            if (HasReachedPageLimit(pageCount, subreddit))
            {
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogDebug("Fetching page {PageNumber} from r/{Subreddit} (after={After})",
                pageCount + 1, subreddit, afterToken ?? "null");

            var page = await _httpClient.GetSubredditPostsAsync(subreddit, afterToken, cancellationToken);
            if (HasNoPosts(page, subreddit))
            {
                break;
            }

            await foreach (var item in GatherItemsFromPostsAsync(subreddit, page.Posts, cancellationToken))
            {
                yield return item;
            }

            if (!TryMoveToNextPage(page, subreddit, ref afterToken, ref pageCount))
            {
                break;
            }

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

            if (HasReachedCommentLimit(commentCount, post.Id))
            {
                break;
            }

            var page = await _httpClient.GetPostCommentsAsync(subreddit, post.Id!, after, cancellationToken);
            if (HasNoComments(page, post.Id))
            {
                break;
            }

            foreach (var comment in page.Comments)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var commentItem = _mapper.Map<CollectorItem>(comment);
                _logger.LogDebug("Collected comment: {CommentId} by {Author}", comment.Id, comment.Author);
                yield return commentItem;

                commentCount++;

                if (HasReachedCommentLimit(commentCount, post.Id))
                {
                    break;
                }
            }

            // Check for next page of comments
            after = page.After;
            if (string.IsNullOrEmpty(after))
            {
                _logger.LogDebug("No more comment pages available for post {PostId}", post.Id);
                break;
            }

            await Task.Delay(_config.RequestDelayMs, cancellationToken);
        }

        _logger.LogDebug("Completed gathering comments for post {PostId}", post.Id);
    }

    private static List<string> GetConfiguredSubreddits() =>
        RedditSubredditCatalog.All
            .Where(static subreddit => !string.IsNullOrWhiteSpace(subreddit))
            .ToList();

    private bool HasNoSubreddits(IReadOnlyCollection<string> subreddits)
    {
        if (subreddits.Count > 0)
        {
            return false;
        }

        _logger.LogWarning("No subreddits configured in the Core subreddit catalog");
        return true;
    }

    private bool HasReachedPageLimit(int pageCount, string subreddit)
    {
        if (!_config.MaxPages.HasValue || _config.MaxPages <= 0 || pageCount < _config.MaxPages)
        {
            return false;
        }

        _logger.LogInformation("Reached maximum page limit ({MaxPages}) for r/{Subreddit}",
            _config.MaxPages, subreddit);
        return true;
    }

    private bool HasNoPosts(RedditPostsPage page, string subreddit)
    {
        if (page.Posts.Count > 0)
        {
            return false;
        }

        _logger.LogInformation("No more posts available from r/{Subreddit}", subreddit);
        return true;
    }

    private async IAsyncEnumerable<ICollectorItem> GatherItemsFromPostsAsync(
        string subreddit,
        IReadOnlyList<RedditPost> posts,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var post in posts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var postItem = _mapper.Map<CollectorItem>(post);
            _logger.LogDebug("Collected post: {PostId} - {Title}", post.Id, post.Title);
            yield return postItem;

            if (_config.FetchComments && post.NumComments > 0)
            {
                await foreach (var commentItem in GatherCommentsForPostAsync(subreddit, post, cancellationToken))
                {
                    yield return commentItem;
                }
            }

            await Task.Delay(_config.RequestDelayMs, cancellationToken);
        }
    }

    private bool TryMoveToNextPage(
        RedditPostsPage page,
        string subreddit,
        ref string? afterToken,
        ref int pageCount)
    {
        afterToken = page.After;
        if (string.IsNullOrEmpty(afterToken))
        {
            _logger.LogInformation("No more pages available for r/{Subreddit}", subreddit);
            return false;
        }

        pageCount++;
        _logger.LogDebug("Moving to next page for r/{Subreddit}", subreddit);
        return true;
    }

    private bool HasReachedCommentLimit(int commentCount, string? postId)
    {
        if (!_config.MaxCommentsPerPost.HasValue ||
            _config.MaxCommentsPerPost <= 0 ||
            commentCount < _config.MaxCommentsPerPost)
        {
            return false;
        }

        _logger.LogDebug(
            "Reached maximum comment limit ({MaxComments}) for post {PostId}",
            _config.MaxCommentsPerPost,
            postId);
        return true;
    }

    private bool HasNoComments(RedditCommentsPage page, string? postId)
    {
        if (page.Comments.Count > 0)
        {
            return false;
        }

        _logger.LogDebug("No more comments available for post {PostId}", postId);
        return true;
    }
}
