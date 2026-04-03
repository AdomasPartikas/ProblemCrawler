using AutoMapper;
using Microsoft.Extensions.Logging;
using ProblemCrawler.Core.Configuration;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Core.Models;
using ProblemCrawler.Core.Models.Reddit;
using ProblemCrawler.Collectors.Reddit.Services;
using ProblemCrawler.Core.Constants;
using ProblemCrawler.Core.Records.Reddit;
using ProblemCrawler.Logging.LoggerMessages;

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

        _logger.LogCollectorStarted(Name, subreddits.Count);

        if (subreddits.Count == 0)
        {
            yield break;
        }

        foreach (var subreddit in subreddits)
        {
            await foreach (var item in GatherFromSubredditAsync(subreddit, cancellationToken))
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Gathers posts from a single subreddit and optionally their comments.
    /// </summary>
    private async IAsyncEnumerable<ICollectorItem> GatherFromSubredditAsync(
        string subreddit,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string? afterToken = null;
        int pageCount = 0;
        int pagesProcessed = 0;
        int yieldedPosts = 0;
        int yieldedComments = 0;

        _logger.LogCollectorSubredditCollectionStarted(Name, subreddit, _config.MaxPages, _config.FetchComments, _config.RequestDelayMs);

        while (!HasReachedPageLimit(pageCount))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = await _httpClient.GetSubredditPostsAsync(subreddit, afterToken, cancellationToken);
            pagesProcessed++;

            _logger.LogCollectorSubredditPageFetched(
                Name,
                subreddit,
                pagesProcessed,
                page.Posts.Count,
                !string.IsNullOrWhiteSpace(page.After));

            if (HasNoPosts(page))
            {
                _logger.LogCollectorSubredditPageEmpty(Name, subreddit, pagesProcessed);
                break;
            }

            await foreach (var item in GatherItemsFromPostsAsync(subreddit, page.Posts, cancellationToken))
            {
                if (string.Equals(item.ItemType, "Post", StringComparison.OrdinalIgnoreCase))
                {
                    yieldedPosts++;
                }
                else if (string.Equals(item.ItemType, "Comment", StringComparison.OrdinalIgnoreCase))
                {
                    yieldedComments++;
                }

                yield return item;
            }

            if (!TryMoveToNextPage(page, ref afterToken, ref pageCount))
            {
                break;
            }

            await Task.Delay(_config.RequestDelayMs, cancellationToken);
        }

        if (HasReachedPageLimit(pageCount))
        {
            _logger.LogCollectorSubredditPageLimitReached(Name, subreddit, pagesProcessed, _config.MaxPages);
        }

        _logger.LogCollectorSubredditCollectionCompleted(Name, subreddit, pagesProcessed, yieldedPosts, yieldedComments);
    }

    /// <summary>
    /// Gathers comments for a specific post.
    /// </summary>
    private async IAsyncEnumerable<ICollectorItem> GatherCommentsForPostAsync(
        string subreddit,
        RedditPost post,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string? after = null;
        int commentCount = 0;
        int commentPage = 0;

        while (!HasReachedCommentLimit(commentCount))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = await _httpClient.GetPostCommentsAsync(subreddit, post.Id!, after, cancellationToken);
            commentPage++;

            _logger.LogCollectorCommentPageFetched(
                Name,
                subreddit,
                post.Id,
                commentPage,
                page.Comments.Count,
                !string.IsNullOrWhiteSpace(page.After));

            if (HasNoComments(page))
            {
                break;
            }

            bool limitReached = false;
            foreach (var comment in page.Comments)
            {
                cancellationToken.ThrowIfCancellationRequested();

                yield return _mapper.Map<CollectorItem>(comment);
                commentCount++;
                _logger.LogCollectorCommentYielded(Name, subreddit, post.Id, comment.Id);

                if (HasReachedCommentLimit(commentCount))
                {
                    _logger.LogCollectorCommentLimitReached(Name, subreddit, post.Id, commentCount, _config.MaxCommentsPerPost);
                    break;
                }

                foreach (var reply in FlattenReplies(comment))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return _mapper.Map<CollectorItem>(reply);
                    commentCount++;
                    _logger.LogCollectorCommentYielded(Name, subreddit, post.Id, reply.Id);

                    if (HasReachedCommentLimit(commentCount))
                    {
                        _logger.LogCollectorCommentLimitReached(Name, subreddit, post.Id, commentCount, _config.MaxCommentsPerPost);
                        limitReached = true;
                        break;
                    }
                }

                if (limitReached)
                    break;
            }

            after = page.After;

            if (string.IsNullOrEmpty(after))
            {
                break;
            }

            await Task.Delay(_config.RequestDelayMs, cancellationToken);
        }
    }

    private static List<string> GetConfiguredSubreddits() =>
        [.. RedditSubredditCatalog.All.Where(static subreddit => !string.IsNullOrWhiteSpace(subreddit))];

    private bool HasReachedPageLimit(int pageCount) =>
         _config.MaxPages.HasValue && _config.MaxPages > 0 && pageCount >= _config.MaxPages;

    private static bool HasNoPosts(RedditPostsPage page) =>
         page.Posts.Count == 0;

    private async IAsyncEnumerable<ICollectorItem> GatherItemsFromPostsAsync(
        string subreddit,
        IReadOnlyList<RedditPost> posts,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var post in posts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var postItem = _mapper.Map<CollectorItem>(post);

            _logger.LogCollectorPostYielded(
                Name,
                subreddit,
                post.Id,
                post.NumComments);

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

    private static bool TryMoveToNextPage(
        RedditPostsPage page,
        ref string? afterToken,
        ref int pageCount)
    {
        afterToken = page.After;

        if (string.IsNullOrEmpty(afterToken))
        {
            return false;
        }

        pageCount++;

        return true;
    }

    private bool HasReachedCommentLimit(int commentCount) =>
         _config.MaxCommentsPerPost.HasValue && _config.MaxCommentsPerPost > 0 && commentCount >= _config.MaxCommentsPerPost;

    private static bool HasNoComments(RedditCommentsPage page) =>
         page.Comments.Count == 0;

    /// <summary>
    /// Recursively flattens all nested replies of a comment into a single sequence.
    /// </summary>
    private static IEnumerable<RedditComment> FlattenReplies(RedditComment comment)
    {
        if (comment.Replies?.Data?.Children is not { Count: > 0 } children)
            yield break;

        foreach (var child in children)
        {
            if (child.Data is not RedditComment reply)
                continue;

            yield return reply;

            foreach (var nested in FlattenReplies(reply))
                yield return nested;
        }
    }
}