using System.Text.Json;
using System.Text.Json.Serialization;
using ProblemCrawler.Core.Enums;
using ProblemCrawler.Core.Models.Reddit;

namespace ProblemCrawler.Collectors.Reddit.Serialization;

internal sealed class RedditChildJsonConverter : JsonConverter<RedditChild>
{
    public override RedditChild Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        var kind = root.TryGetProperty("kind", out var kindElement)
            ? kindElement.GetString()
            : null;

        RedditChildData? data = null;
        if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Object)
        {
            data = kind switch
            {
                var value when value.IsApiKind(RedditObjectKind.Post) => dataElement.Deserialize<RedditPost>(options),
                var value when value.IsApiKind(RedditObjectKind.Comment) => dataElement.Deserialize<RedditComment>(options),
                _ => dataElement.Deserialize<RedditChildData>(options)
            };
        }

        return new RedditChild
        {
            Kind = kind,
            Data = data
        };
    }

    public override void Write(Utf8JsonWriter writer, RedditChild value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Serialization is not supported for RedditChild.");
}
