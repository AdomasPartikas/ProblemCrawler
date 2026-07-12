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
                - The problem is concrete enough to identify the affected actor, the operational pain, and either the current workaround or desired outcome.

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
                - If softwareOpportunity is false or isActionable is false, then containsProblem must also be false.
                - If containsProblem is true, then actor and problemDetails must be non-null.
                - If containsProblem is true, at least one of desiredOutcome or currentWorkaround must be non-null.
                - If containsProblem is true, actionabilityRationale must be non-null and describe a specific product or workflow solution, not a vague statement that "tools could help".
                - isActionable must be true ONLY when a specific, buildable software product could realistically solve this problem commercially. A generic statement like "tools could help" does not qualify. Describe the concrete product opportunity in actionabilityRationale.
                - softwareOpportunity should be true only when you can reasonably envision a software product addressing this problem.

                CURRENT ITEM:
                Type: {{context.Current.ItemType}}
                Title: {{context.Current.Title}}
                Content:
                {{context.Current.Content}}
                """;

        if (context.Post is not null)
        {
            prompt += $$"""

                POST CONTEXT:
                Title: {{context.Post.Title}}
                {{context.Post.Content}}
                """;
        }

        if (context.Parent is not null)
        {
            prompt += $$"""

                PARENT COMMENT CONTEXT:
                Title: {{context.Parent.Title}}
                {{context.Parent.Content}}
                """;
        }

        return prompt;
    }

    public static string BuildRepairPrompt(string invalidResponse, string error)
    {
        return $$"""
        Your previous JSON output failed validation.
        Error: {{error}}

        Previous output:
        {{invalidResponse}}

        Return corrected JSON only. No markdown, no extra text.
        """;
    }

    public static string BuildThreadSynthesisPrompt(ThreadSynthesisContext context)
    {
        var evidence = string.Join(
            "\n\n",
            context.Items.Select((item, index) => $$"""
                Evidence Item {{index + 1}}
                - EvidenceNumber: {{index + 1}}
                - SourceId: {{item.SourceId}}
                - ItemType: {{item.ItemType}}
                - Author: {{item.Author ?? "unknown"}}
                - CreatedAtUtc: {{item.CreatedAtUtc:O}}
                - ProblemSummary: {{item.ProblemSummary}}
                - ProblemDetails: {{item.ProblemDetails ?? "null"}}
                - Actor: {{item.Actor ?? "null"}}
                - Industry: {{item.Industry}}
                - CurrentWorkaround: {{item.CurrentWorkaround ?? "null"}}
                - DesiredOutcome: {{item.DesiredOutcome ?? "null"}}
                - UrgencySignal: {{item.UrgencySignal}}
                - ActionabilityRationale: {{item.ActionabilityRationale ?? "null"}}
                """));

        return $$"""
            You are synthesizing a Reddit thread into UNIQUE software opportunity ideas.

            The input items below are already filtered to problem-focused, actionable evidence from the SAME root thread.
            Your job is to merge near-duplicate evidence from the same thread into distinct ideas so one busy thread does not produce many copies of the same opportunity.

            IMPORTANT: If an evidence item is just someone sharing a working solution or tip (not describing an unresolved problem they face), do NOT synthesize an idea from it.

            Return ONLY valid JSON with this exact shape:
            {
                "ideas": [
                    {
                        "problemSummary": "one sentence summary of the unique idea",
                        "problemDetails": null,
                        "actor": "person experiencing the problem",
                        "industry": "short free-text industry label",
                        "currentWorkaround": null,
                        "desiredOutcome": null,
                        "urgencySignal": "low | medium | high",
                        "softwareOpportunity": true,
                        "isActionable": true,
                        "actionabilityRationale": "specific product or workflow opportunity",
                                    "supportingEvidenceNumbers": [1, 3]
                    }
                ]
            }

            Rules:
            - Return JSON only. No markdown code fences, no explanation outside the JSON.
            - Zero ideas is allowed. Use an empty array when the thread does not support a unique actionable software opportunity after deduplication.
            - If there is no genuine unresolved problem (e.g., someone is sharing a tip that works), return an empty ideas array instead of synthesizing an idea.
            - Do NOT emit one idea per comment. Merge semantically similar evidence into a single idea.
            - Keep genuinely different problems separate, even if they appear in the same thread.
            - Every returned idea must represent an unresolved problem that could plausibly be solved by a specific software product.
            - problemSummary and industry are required and must be non-empty.
            - urgencySignal must be exactly one of: low, medium, high.
            - actor, problemDetails, and actionabilityRationale are required for every returned idea. Never omit these or use placeholder text like "null", "n/a", "unknown", or "not specified".
            - At least one of desiredOutcome or currentWorkaround must be non-null for every returned idea. Never use placeholder text like "null" for these fields.
            - softwareOpportunity must always be true for returned ideas.
            - isActionable must always be true for returned ideas.
            - supportingEvidenceNumbers must be a non-empty array of unique EvidenceNumber values from the evidence list below.
            - Do not invent evidence numbers.
            - The application will calculate support counts from the evidence numbers you provide.

            ROOT THREAD:
            Type: {{context.Root.ItemType}}
            Title: {{context.Root.Title}}
            Content:
            {{context.Root.Content}}

            EVIDENCE ITEMS:
            {{evidence}}
            """;
    }
}
