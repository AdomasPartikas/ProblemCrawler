namespace ProblemCrawler.Core.Configuration;

/// <summary>
/// Centralized word lists used by the filtering stage.
/// </summary>
public static class FilteringWordLists
{
    public static HashSet<string> DeletedMarkers { get; } =
    [
        "[deleted]",
        "[removed]"
    ];

    public static HashSet<string> RemovedWordList { get; } =
    [
        "k",
        "ok",
        "okay",
        "yes",
        "no",
        "same",
        "lol",
        "lmao",
        "idk",
        "thx",
        "thanks",
        "following",
        "+1"
    ];
}
