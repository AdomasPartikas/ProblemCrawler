using System.Runtime.Serialization;
using ProblemCrawler.Core.Extensions;

namespace ProblemCrawler.Core.Enums;

/// <summary>
/// Represents the type of Reddit object in the API response.
/// t3 = Post/Link (Submission)
/// t1 = Comment
/// t2 = Account/User
/// </summary>
public enum RedditObjectKind
{
    /// <summary>Post or Link submission</summary>
    [EnumMember(Value = "t3")]
    Post, // t3

    /// <summary>Comment on a post</summary>
    [EnumMember(Value = "t1")]
    Comment, // t1

    /// <summary>User/Account</summary>
    [EnumMember(Value = "t2")]
    User // t2
}

public static class RedditObjectKindExtensions
{
    public static bool IsApiKind(this string? value, RedditObjectKind expectedKind) =>
        string.Equals(value, expectedKind.ToApiValue(), StringComparison.OrdinalIgnoreCase);
}
