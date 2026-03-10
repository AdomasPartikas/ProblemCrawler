## Plan: ProblemCrawler MVP Pipeline

Build a modular, source-agnostic pipeline where Reddit is the first collector implementation and every downstream stage works against a shared normalized model. The recommended v1 architecture is a .NET 10 solution split into API orchestration, domain contracts, collector, parser, analysis, and persistence modules backed by PostgreSQL and Ollama. Scope is the first complete vertical slice: gather -> parse -> save raw/normalized -> analyze (LLM + scoring + clustering) -> save analyzed -> expose read endpoints.

**Steps**
1. Phase 1 - Solution and Architecture Skeleton
1.1 Add class library projects for Core contracts/models, Infrastructure/Persistence, and Pipeline services; keep API as orchestration host. *blocks all later phases*
1.2 Define stage contracts in Core (collector, parser, analyzer, scorer/clusterer, repository abstractions) and canonical ingestion model so new sources can plug in without parser changes. *depends on 1.1*
1.3 Define configuration contracts for Reddit, Ollama, scheduling, and scoring thresholds. *depends on 1.1; parallel with 1.2*

2. Phase 2 - Data Model and Persistence (PostgreSQL)
2.1 Design relational schema for raw documents, normalized problem candidates, analysis results, clusters, and opportunity scores with lineage fields linking every stage output to source records. *depends on Phase 1*
2.2 Implement EF Core DbContext, entity mappings, migrations, and repositories; include idempotency keys to avoid duplicate imports. *depends on 2.1*
2.3 Add Docker Compose services for PostgreSQL and API, with connection and health check config. *parallel with 2.2 once schema is stable*

3. Phase 3 - Gathering and Parsing Pipeline
3.1 Implement Reddit collector adapter (first source) that fetches target subreddits and maps to canonical raw model. *depends on Phase 1 and 2*
3.2 Implement parsing/normalization stage and keyword prefilter (pain indicators, opportunity indicators, manual-work indicators). *depends on 3.1; can run independently of LLM stage*
3.3 Persist both raw gathered content and parsed candidates with processing status transitions. *depends on 3.1 and 3.2*

4. Phase 4 - LLM Analysis, Scoring, and Clustering
4.1 Implement Ollama analysis client with strict JSON contract and retry/backoff handling. *depends on 3.2*
4.2 Add analysis stage that enriches parsed candidates into structured opportunity records (problem, industry, software fit, summary). *depends on 4.1*
4.3 Add deterministic scoring model (mentions, engagement, cross-subreddit coverage, recency) and initial clustering strategy (keyword plus optional embedding-ready abstraction). *depends on 4.2*
4.4 Persist analyzed records, score history, and cluster assignments. *depends on 4.2 and 4.3*

5. Phase 5 - Orchestration, Scheduling, and API Surface
5.1 Implement scheduled batch runner (hourly default) using hosted background service; add safe re-entry and batch checkpointing. *depends on Phases 2-4*
5.2 Add API endpoints for top opportunities, clusters, and example source posts with filters by date/subreddit/industry. *depends on 4.4*
5.3 Add operational endpoints for pipeline run status and recent errors. *depends on 5.1*

6. Phase 6 - Quality, Observability, and Hardening
6.1 Add structured logging and correlation IDs across pipeline stages. *parallel with 5.2*
6.2 Add unit tests for parser, scoring, and clustering; add integration tests for database persistence and Ollama contract handling with mocks. *depends on Phases 3-5*
6.3 Add seed configuration for subreddit lists and keyword dictionaries, plus documentation for extending new gatherers. *depends on final model and contracts*

**Relevant files**
- c:/Users/Patrikas/source/repos/ProblemCrawler/ProblemCrawler.sln - Add new projects and references.
- c:/Users/Patrikas/source/repos/ProblemCrawler/ProblemCrawler.API/Program.cs - Convert from hello-world entrypoint to DI registration, hosted pipeline scheduler, and read endpoints.
- c:/Users/Patrikas/source/repos/ProblemCrawler/ProblemCrawler.API/appsettings.json - Add Reddit, PostgreSQL, Ollama, schedule, scoring, and clustering configs.
- c:/Users/Patrikas/source/repos/ProblemCrawler/ProblemCrawler.API/appsettings.Development.json - Development overrides for local services.
- c:/Users/Patrikas/source/repos/ProblemCrawler/ProblemCrawler.API/ProblemCrawler.API.csproj - Add project/package references.
- c:/Users/Patrikas/source/repos/ProblemCrawler/ProblemCrawler.Core (new) - Canonical models and stage interfaces.
- c:/Users/Patrikas/source/repos/ProblemCrawler/ProblemCrawler.Infrastructure (new) - EF Core DbContext, migrations, repositories.
- c:/Users/Patrikas/source/repos/ProblemCrawler/ProblemCrawler.Pipeline (new) - Collector/parser/analyzer/scoring/clustering services.
- c:/Users/Patrikas/source/repos/ProblemCrawler/docker-compose.yml (new) - PostgreSQL + API + optional Ollama container wiring.
- c:/Users/Patrikas/source/repos/ProblemCrawler/README.md (new or updated) - Runbook, schedule behavior, extension guide for new data sources.

**Verification**
1. Build validation: run solution build and verify all projects compile.
2. Migration validation: create and apply migrations against local PostgreSQL; verify tables and constraints for each stage output.
3. Pipeline dry run: execute one scheduled cycle manually against a small subreddit subset and confirm stage transitions gather -> parse -> analyze -> score/cluster -> persist.
4. API validation: query top opportunities/clusters endpoints and confirm records match stored analysis results.
5. Resilience checks: simulate Ollama timeout and Reddit fetch error; verify retry policy, status tracking, and non-destructive partial-failure behavior.
6. Idempotency checks: rerun same ingestion window and verify duplicates are not reinserted.

**Decisions**
- Included scope: Reddit-only v1 collector, PostgreSQL persistence, Ollama local LLM, scheduled batch processing, read API endpoints.
- Excluded scope for v1: dashboard UI, non-Reddit collectors, advanced embedding/vector search infrastructure, cloud deployment automation.
- Naming: keep project name ProblemCrawler for now; architecture supports future branding without schema breakage.
- Design principle: source adapters map into one canonical model so parser and all downstream logic remain source-agnostic.

**Further Considerations**
1. Clustering strategy: Option A keyword-based only (fastest MVP), Option B keyword plus embedding abstraction without immediate vector DB (recommended), Option C full embeddings plus vector DB now.
2. Scheduling cadence: Option A hourly default (recommended), Option B every 30 minutes, Option C configurable per source.
3. Ollama model baseline: Option A smaller model for speed, Option B stronger model for quality (recommended if local hardware allows).