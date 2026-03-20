using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProblemCrawler.Core.Configuration;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Pipeline.Interfaces;

namespace ProblemCrawler.Pipeline.Services;

public sealed class LLMAnalysisSchedulerTask(
    IServiceScopeFactory scopeFactory,
    IOptions<LLMAnalysisSchedulingConfiguration> options,
    ILogger<LLMAnalysisSchedulerTask> logger) : ILLMAnalysisSchedulerTask
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly LLMAnalysisSchedulingConfiguration _options = options.Value;
    private readonly ILogger<LLMAnalysisSchedulerTask> _logger = logger;
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
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ILLMAnalysisService>();
            var summary = await service.ExecuteAsync(CancellationToken.None);

            _logger.LogInformation(
                "LLM analysis run completed. Evaluated: {Evaluated}, analysed: {Analysed}, skipped: {Skipped}, failed: {Failed}",
                summary.Evaluated,
                summary.Analysed,
                summary.Skipped,
                summary.Failed);
        }
        finally
        {
            if (!_options.AllowConcurrentRuns)
            {
                _runLock.Release();
            }
        }
    }
}
