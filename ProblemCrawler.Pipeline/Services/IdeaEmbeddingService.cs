using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProblemCrawler.Core.Configuration;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Core.Records.Embedding;
using ProblemCrawler.Pipeline.Clients;

namespace ProblemCrawler.Pipeline.Services
{
    public sealed class IdeaEmbeddingService(
        ICollectorItemRepository repository,
        IOptions<OllamaConfiguration> ollamaOptions,
        IOptions<EmbeddingConfiguration> embeddingOptions,
        OllamaHttpClient ollamaHttpClient,
        ILogger<IdeaEmbeddingService> logger) : IIdeaEmbeddingService
    {
        private readonly ICollectorItemRepository _repository = repository;
        private readonly OllamaConfiguration _ollamaOptions = ollamaOptions.Value;
        private readonly ILogger<IdeaEmbeddingService> _logger = logger;
        private readonly EmbeddingConfiguration _embeddingOptions = embeddingOptions.Value;
        private readonly OllamaHttpClient _ollamaHttpClient = ollamaHttpClient;
        public async Task<EmbeddingRunSummary> ExecuteAsync(CancellationToken cancellationToken)
        {
            var evaluated = 0;
            var embedded = 0;
            var skipped = 0;
            var failed = 0;

            if (_embeddingOptions.BatchSize <= 0)
            {
                throw new InvalidOperationException("EmbeddingConfiguration.BatchSize must be greater than 0.");
            }
            var batchSize = _embeddingOptions.BatchSize;

            while (!cancellationToken.IsCancellationRequested)
            {
                var candidates = await _repository.GetEmbeddingCandidatesAsync(batchSize, _ollamaOptions.EmbedModel, cancellationToken);
                if (candidates is null || candidates.Count == 0)
                {
                    break;
                }

                var batch = new List<EmbeddingUpsert>(candidates.Count);

                foreach (var candidate in candidates)
                {
                    evaluated++;
                    var text = BuildEmbedText(candidate);

                    var floats = await _ollamaHttpClient.EmbedAsync(text, cancellationToken);
                    if (floats is null)
                    {
                        _logger.LogWarning("Embedding failed for idea {ideaId}", candidate.IdeaId);
                        failed++;
                        continue;
                    }

                    if (floats.Length == 0)
                    {
                        _logger.LogWarning("Empty embedding returned for idea {IdeaId}", candidate.IdeaId);
                        skipped++;
                        continue;
                    }


                    batch.Add(new EmbeddingUpsert(candidate.IdeaId, _ollamaOptions.EmbedModel, floats, DateTime.UtcNow));
                    embedded++;
                }
                if (batch.Count > 0)
                {
                    try
                    {
                        await _repository.UpsertEmbeddingAsync(batch, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save embedding batch of {Count} ideas", batch.Count);
                        embedded -= batch.Count;
                        failed += batch.Count;
                    }
                }
                if (candidates.Count < batchSize)
                {
                    break;
                }
            }

            return new EmbeddingRunSummary(evaluated, embedded, skipped, failed);


        }

        private static string BuildEmbedText(EmbeddingCandidate c)
        {
            var parts = new List<string>
            {
                c.ProblemSummary
            };

            if (!string.IsNullOrWhiteSpace(c.ProblemSummary))
                parts.Add($"Problem: {c.ProblemSummary}");

            if (!string.IsNullOrWhiteSpace(c.ProblemDetails))
                parts.Add($"Details: {c.ProblemDetails}");

            if (!string.IsNullOrWhiteSpace(c.Actor))
                parts.Add($"Actor: {c.Actor}");

            if (!string.IsNullOrWhiteSpace(c.Industry))
                parts.Add($"Industry: {c.Industry}");

            if (!string.IsNullOrWhiteSpace(c.CurrentWorkaround))
                parts.Add($"Workaround: {c.CurrentWorkaround}");

            if (!string.IsNullOrWhiteSpace(c.DesiredOutcome))
                parts.Add($"Outcome: {c.DesiredOutcome}");

            return string.Join(". ", parts);
        }
    }
}
