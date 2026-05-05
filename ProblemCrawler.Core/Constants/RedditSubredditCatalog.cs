namespace ProblemCrawler.Core.Constants;

/// <summary>
/// Core-owned list of subreddits targeted by the Reddit collector.
/// This keeps source selection separate from runtime collector behavior.
/// </summary>
public static class RedditSubredditCatalog
{
    public static IReadOnlyList<string> All { get; } =
    [
        "freelancing",
        "Startup_Ideas",
        "Entrepreneur",
        "smallbusiness",
        "startups",
        "sidehustle",
        "sideproject",
        "indiehackers",
        "EntrepreneurRideAlong",
        "AskEntrepreneurs",
        "ProblemSolving",
        "Showerthoughts",
        "SomebodyMakeThis",
        "CrazyIdeas",
        "DoesAnybodyElse",
        "TooAfraidToAsk",
        "digitalnomad",
        "WorkOnline",
        "remotejs",
        "remotework",
        "jobs",
        "careerguidance",
        "productivity",
        "GetMotivated",
        "antiwork",
        "overemployed",
        "mildlyinfuriating",
        "personalfinance",
        "nostupidquestions",
        "lifehacks",
        "LifeProTips",
        "firstworldproblems",
        "answers",
        "devops",
        "learnprogramming",
        "softwareengineering",
        "SaaS",
        "webdev"
    ];
}