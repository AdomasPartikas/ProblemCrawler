using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProblemCrawler.Core.Configuration;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Pipeline.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Pipeline.Services
{
    public sealed class IdeaEmbeddingSchedulerTask(
        IServiceScopeFactory scopeFactory,
        ILogger<IdeaEmbeddingSchedulerTask> logger,
        IOptions<EmbeddingSchedulingConfiguration> options) : IIdeaEmbeddingSchedulerTask
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly ILogger<IdeaEmbeddingSchedulerTask> _logger = logger;
        private readonly EmbeddingSchedulingConfiguration _options = options.Value;
        private readonly SemaphoreSlim _runLock = new(1, 1);
        public async Task ExecuteAsync()
        {
            if (!_options.AllowConcurrentRuns)
            {
                var lockAcquired = await _runLock.WaitAsync(0);
                if (!lockAcquired)
                {
                    return;
                }
            }
            try
            {
                await ExecuteRunAsync();
            }
            finally
            {
                if (!_options.AllowConcurrentRuns)
                {
                    _runLock.Release();
                }
            }
        }
        private async Task ExecuteRunAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IIdeaEmbeddingService>();
            var summary = await service.ExecuteAsync(CancellationToken.None);

            _logger.LogInformation(
                "Idea embedding run completed. Evaluated: {Evaluated}, embedded: {Embedded}, skipped: {Skipped}, failed: {Failed}",
                summary.Evaluated, summary.Embedded, summary.Skipped, summary.Failed);
        }

    }
}
