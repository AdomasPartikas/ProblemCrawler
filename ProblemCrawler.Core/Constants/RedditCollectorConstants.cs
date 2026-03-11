namespace ProblemCrawler.Core.Constants;

public static class RedditCollectorDefaults
{
    public const int RequestDelayMs = 500;
    public const int MaxRetries = 3;
    public const int RequestTimeoutMs = 10000;
    public const string UserAgent = "ProblemCrawler/1.0 (+https://github.com/yourusername/ProblemCrawler)";
    public const string BaseUrl = "https://www.reddit.com";
}