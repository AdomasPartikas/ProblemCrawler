using ProblemCrawler.Core.Records.LLM;

namespace ProblemCrawler.Pipeline.Prompts;

public static class LLMAnalysisPromptBuilder
{
    public static string BuildInitialPrompt(LLMAnalysisContext context)
    {
        var prompt = $$"""
                You are analyzing internet posts to identify genuine, unresolved problems that a software startup could build a product to solve.

                We are looking for problems where ALL of the following are true:
                - The author or their community is actively experiencing an ongoing, unsolved friction or pain point.
                - A software product or digital service could realistically be built to address it.
                - People would plausibly pay money for such a solution.

                Do NOT set containsProblem to true for any of these cases:
                - The author is sharing a solution that already works for them (a success story, tip, or brag post).
                - A hiring post or service request (someone looking to hire a developer, designer, freelancer, etc.).
                - Personal mindset, confidence, or psychological barriers (e.g. fear of cold outreach, imposter syndrome).
                - Travel, lifestyle, relationship, or visa issues where no software product is a realistic solution.
                - General advice-seeking where the answer is human coaching, habit change, or interpersonal skills.
                - Posts that are purely a question, a comment, or a reply with no describable pain ("what do you all think?").
                - Off-topic content (music, personal milestones, product announcements not tied to a problem).

                Return ONLY valid JSON with this exact shape:
                {
                  "containsProblem": true,
                  "problemSummary": "one sentence describing what this post is about — ALWAYS REQUIRED even if containsProblem is false",
                  "problemDetails": null,
                  "actor": null,
                  "industry": "short free-text industry label",
                  "currentWorkaround": null,
                  "desiredOutcome": null,
                  "urgencySignal": "low | medium | high",
                  "softwareOpportunity": true,
                  "isActionable": true,
                  "actionabilityRationale": null
                }

                Rules:
                - Return JSON only. No markdown code fences, no explanation outside the JSON.
                - problemSummary is REQUIRED and MUST always be a non-empty sentence describing the post content, even when containsProblem is false. Examples for rejected posts: "User sharing a tax savings tip that already works for them.", "Job posting for a React developer.", "Freelancer asking the community for opinions."
                - industry and urgencySignal are always required. industry is free text (e.g. "freelance services", "cybersecurity"). urgencySignal must be exactly one of: low, medium, high.
                - All other optional fields (problemDetails, actor, currentWorkaround, desiredOutcome, actionabilityRationale) MUST be null — never an empty string "" — when they have no meaningful content.
                - actor must be the type of person experiencing the problem (e.g. "freelancer", "accountant"), not the cause of it. Set to null when containsProblem is false.
                - If containsProblem is false: softwareOpportunity MUST be false, isActionable MUST be false, and actor, problemDetails, currentWorkaround, desiredOutcome, actionabilityRationale MUST all be null.
                - isActionable must be true ONLY when a specific, buildable software product could realistically solve this problem commercially. A generic statement like "tools could help" does not qualify. Describe the concrete product opportunity in actionabilityRationale.
                - softwareOpportunity should be true only when you can reasonably envision a software product addressing this problem.

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
