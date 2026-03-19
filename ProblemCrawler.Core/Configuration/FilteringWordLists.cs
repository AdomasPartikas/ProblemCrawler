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
        "+1",
        "yep",
        "yeah",
        "nah",
        "nope",
        "true",
        "false",
        "maybe",
        "possibly",
        "agreed",
        "this",
        "this.",
        "this!",
        "same here",
        "same!",
        "same lol",
        "same haha",
        "me too",
        "me2",
        "ditto",
        "exactly",
        "100%",
        "facts",
        "fr",
        "for real",
        "lolol",
        "haha",
        "hahaha",
        "hehe",
        "nice",
        "cool",
        "great",
        "awesome",
        "interesting",
        "good",
        "bad",
        "wow",
        "wtf",
        "lmaooo",
        "ok thanks",
        "thanks!",
        "thank you",
        "ty",
        "ty!",
        "appreciate it",
        "following this",
        "bump",
        "up",
        "any updates?",
        "update?",
        "?"
    ];
}
