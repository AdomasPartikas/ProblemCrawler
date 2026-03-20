using ProblemCrawler.Core.Records.LLM;

namespace ProblemCrawler.Pipeline.Prompts;

public static class LLMAnalysisPromptBuilder
{
    public static string BuildInitialPrompt(LLMAnalysisContext context)
    {
        var prompt = $$"""
                Analyze this text and return ONLY valid JSON with this shape:
                {
                "containsProblem": true,
                "problemSummary": "...",
                    "problemDetails": "...",
                    "actor": "...",
                "industry": "short free-text industry label",
                    "currentWorkaround": "...",
                    "desiredOutcome": "...",
                "painLevel": 1,
                    "urgencySignal": "low | medium | high",
                "softwareOpportunity": true,
                "isActionable": true,
                    "actionabilityRationale": "...",
                "confidence": 0.0
                }

                Rules:
                - Return JSON only, no markdown, no explanation.
                - Keep industry as free text.
                - Respect ranges for painLevel [1..5] and confidence [0..1].
                - If containsProblem is false, then isActionable should be false.

                CURRENT ITEM:
                Type: {{context.Current.ItemType}}
                Content:
                {{context.Current.Content}}
                """;

        if (context.Post is not null)
        {
            prompt += $$"""

                POST CONTEXT:
                {{context.Post.Content}}
                """;
        }

        if (context.Parent is not null)
        {
            prompt += $$"""

                PARENT COMMENT CONTEXT:
                {{context.Parent.Content}}
                """;
        }

        return prompt;
    }

    public static string BuildRepairPrompt(string originalPrompt, string invalidResponse, string error)
    {
        return $$"""
            Your previous output was invalid.
            Validation error: {{error}}

            Original task:
            {{originalPrompt}}

            Previous invalid output:
            {{invalidResponse}}

            Return corrected JSON only. No markdown and no extra text.
            """;
    }
}
