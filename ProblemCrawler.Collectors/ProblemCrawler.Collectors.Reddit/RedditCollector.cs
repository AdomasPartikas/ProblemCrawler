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

        while (!HasReachedPageLimit(pageCount))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = await _httpClient.GetSubredditPostsAsync(subreddit, afterToken, cancellationToken);

            if (HasNoPosts(page))
            {
                break;
            }

            await foreach (var item in GatherItemsFromPostsAsync(subreddit, page.Posts, cancellationToken))
            {
                yield return item;
            }

            if (!TryMoveToNextPage(page, ref afterToken, ref pageCount))
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
        string? after = null;
        int commentCount = 0;

        while (!HasReachedCommentLimit(commentCount))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = await _httpClient.GetPostCommentsAsync(subreddit, post.Id!, after, cancellationToken);

            if (HasNoComments(page))
            {
                break;
            }

            foreach (var comment in page.Comments)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var commentItem = _mapper.Map<CollectorItem>(comment);

                yield return commentItem;

                commentCount++;

                if (HasReachedCommentLimit(commentCount))
                {
                    break;
                }
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
         !_config.MaxPages.HasValue || _config.MaxPages <= 0 || pageCount < _config.MaxPages;

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
         !_config.MaxCommentsPerPost.HasValue || _config.MaxCommentsPerPost <= 0 || commentCount < _config.MaxCommentsPerPost;

    private static bool HasNoComments(RedditCommentsPage page) =>
         page.Comments.Count == 0;
}
