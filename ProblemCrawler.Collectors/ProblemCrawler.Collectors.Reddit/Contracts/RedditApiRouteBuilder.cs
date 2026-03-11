namespace ProblemCrawler.Collectors.Reddit.Contracts;

internal static class RedditApiRouteBuilder
{
    private const string TimeRangeKey = "t";
    private const string AfterKey = "after";

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
            query.Add($"{TimeRangeKey}={Uri.EscapeDataString(timeRange)}");
        }

        if (!string.IsNullOrWhiteSpace(after))
        {
            query.Add($"{AfterKey}={Uri.EscapeDataString(after)}");
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

        return $"{url}?{AfterKey}={Uri.EscapeDataString(after)}";
    }
}