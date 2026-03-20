# Plan: LLMAnalysis Stage (Ollama + Context-Aware JSON)

Add a second analysis stage that takes items in ReadyForAnalysis, calls Ollama through a configurable URL, enforces strict JSON output against a typed contract, stores results in a dedicated analysis table linked to each item, and moves stage to Analysed only on valid success.
Per your decisions: dedicated table, new conversation per item, post/direct-parent context only, multiple JSON repair attempts, max 3 retries then keep ReadyForAnalysis and skip.

**Steps**

Phase 1: Core contracts and config
Add LLM analysis DTOs/config types (settings + scheduling + run summary + update records).
Add service/scheduler interfaces for the new stage.
Phase 2: Persistence and repository extensions
Add a new analysis entity/table with 1:1 relation to collector item and raw JSON storage.
Extend repository for: fetching LLM candidates, resolving context inputs, and persisting success/failure outcomes with stage updates.
Add EF config + migration + indexes (collector item link, actionable, industry, confidence, analyzed timestamp).
Phase 3: Ollama client and prompt/response handling
Add typed HTTP client with configurable base URL/model/timeout/retries.
Add prompt builder with context rules:
Post: analyze self.
Comment where ParentId = LinkId: include post context.
Comment where ParentId != LinkId: include parent comment + post.
Add strict JSON validation + multi-attempt repair loop.
Phase 4: Pipeline execution + scheduling
Implement batch LLMAnalysis service mirroring filtering flow.
Add scheduler task mirroring filtering scheduler lock/scope behavior.
Wire DI + Hangfire recurring job + appsettings sections.
Apply retry policy: success => Analysed; failed after capped attempts => remain ReadyForAnalysis and skipped for now.
Phase 5: Lightweight manual endpoint
Add one-item trigger by collector item ID using the same analysis path as scheduled execution.
Return compact execution/result payload for local testing.
Phase 6: Verification
Unit tests for context assembly and JSON repair/validation.
Integration tests for stage transitions and result persistence.
End-to-end dry run against local/remote Ollama URL.

**Relevant Existing Files**

FilteringService.cs
FilteringSchedulerTask.cs
ServiceCollectionExtensions.cs
ServiceCollectionExtensions.cs
ApplicationBuilderExtensions.cs
Program.cs
appsettings.json
appsettings.Development.json
ICollectorItemRepository.cs
CollectorItemRepository.cs
ProblemCrawlerDbContext.cs
RedditHttpClient.cs

**Verification**

Build solution
Apply migration and verify new analysis table/indexes.
Run one-item endpoint for a known ReadyForAnalysis item and verify table row + stage transition.
Test reply-comment case (ParentId != LinkId) and verify parent+post context inclusion.
Force invalid JSON and verify repair attempts + final retry behavior.
Run scheduled job and verify batch/retry/skip semantics.