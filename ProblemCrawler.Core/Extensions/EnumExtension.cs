using System.Reflection;
using System.Runtime.Serialization;

namespace ProblemCrawler.Core.Extensions;

public static class EnumExtension
{
    /// <summary>
    /// Gets the string value of an enum member, using the EnumMember attribute if present.
    /// </summary>
    public static string ToApiValue<TEnum>(this TEnum value) where TEnum : struct, Enum
    {
        var member = typeof(TEnum).GetMember(value.ToString()).FirstOrDefault();
        if (member is null)
        {
            return value.ToString().ToLowerInvariant();
        }

        var enumMember = member.GetCustomAttribute<EnumMemberAttribute>();
        return enumMember?.Value ?? value.ToString().ToLowerInvariant();
    }
}