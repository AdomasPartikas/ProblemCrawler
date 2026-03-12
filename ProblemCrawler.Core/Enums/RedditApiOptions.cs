using System.Runtime.Serialization;

namespace ProblemCrawler.Core.Enums;

public enum RedditSort
{
    [EnumMember(Value = "hot")]
    Hot,

    [EnumMember(Value = "new")]
    New,

    [EnumMember(Value = "top")]
    Top,

    [EnumMember(Value = "rising")]
    Rising,

    [EnumMember(Value = "controversial")]
    Controversial
}

public enum RedditTimeRange
{
    [EnumMember(Value = "all")]
    All,

    [EnumMember(Value = "day")]
    Day,

    [EnumMember(Value = "week")]
    Week,

    [EnumMember(Value = "month")]
    Month,

    [EnumMember(Value = "year")]
    Year
}