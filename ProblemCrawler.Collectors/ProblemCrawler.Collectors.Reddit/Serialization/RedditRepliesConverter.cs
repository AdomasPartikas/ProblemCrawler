using System.Text.Json;
using System.Text.Json.Serialization;
using ProblemCrawler.Core.Models.Reddit;

namespace ProblemCrawler.Collectors.Reddit.Serialization;

/// <summary>
/// Handles deserialization of the Reddit "replies" field, which can be either:
/// - An empty string ("") when there are no replies
/// - A full Listing object when replies exist
/// </summary>
internal sealed class RedditRepliesConverter : JsonConverter<RedditReplies>
{
    public override RedditReplies? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.String or JsonTokenType.Null)
            return null;

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        var kind = root.TryGetProperty("kind", out var kindElement)
            ? kindElement.GetString()
            : null;

        RedditListingData? data = null;
        if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Object)
            data = dataElement.Deserialize<RedditListingData>(options);

        return new RedditReplies { Kind = kind, Data = data };
    }

    public override void Write(Utf8JsonWriter writer, RedditReplies value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Serialization of RedditReplies is not supported.");
}
