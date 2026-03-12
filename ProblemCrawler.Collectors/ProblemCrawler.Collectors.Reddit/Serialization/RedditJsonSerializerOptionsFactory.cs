using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProblemCrawler.Collectors.Reddit.Serialization;

internal static class RedditJsonSerializerOptionsFactory
{
    public static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        options.Converters.Add(new RedditChildJsonConverter());
        options.Converters.Add(new RedditRepliesConverter());
        return options;
    }
}
