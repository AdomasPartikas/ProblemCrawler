using ProblemCrawler.Core.Constants;

namespace ProblemCrawler.Collectors.Reddit.Contracts;

internal static class RedditApiRouteBuilder
{
    public static string BuildSubredditUrl(
        string baseUrl,
        string subreddit,
        string sort,
        string? timeRange,
        string? after)
    {
        var url = $"{baseUrl.TrimEnd('/')}/r/{subreddit}/{sort}.json";
        var query = new List<string>();

        if (!string.IsNullOrWhiteSpace(timeRange))
        {
            query.Add($"{RedditCollectorContractConstants.TimeRangeKey}={Uri.EscapeDataString(timeRange)}");
        }

        if (!string.IsNullOrWhiteSpace(after))
        {
            query.Add($"{RedditCollectorContractConstants.AfterKey}={Uri.EscapeDataString(after)}");
        }

        return query.Count == 0
            ? url
            : $"{url}?{string.Join("&", query)}";
    }

    public static string BuildPostCommentsUrl(string baseUrl, string subreddit, string postId, string? after)
    {
        var url = $"{baseUrl.TrimEnd('/')}/r/{subreddit}/comments/{postId}.json";

        if (string.IsNullOrWhiteSpace(after))
        {
            return url;
        }

        return $"{url}?{RedditCollectorContractConstants.AfterKey}={Uri.EscapeDataString(after)}";
    }
}