using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProblemCrawler.Core.Configuration;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Pipeline.Interfaces;
using ProblemCrawler.Logging.LoggerMessages;

namespace ProblemCrawler.Pipeline.Services;

/// <summary>
/// Runs the filtering stage for a scheduled job execution.
/// </summary>
public sealed class FilteringSchedulerTask(
    IServiceScopeFactory scopeFactory,
    IOptions<FilteringSchedulingConfiguration> filteringOptions,
    ILogger<FilteringSchedulerTask> logger) : IFilteringSchedulerTask
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly FilteringSchedulingConfiguration _filteringOptions = filteringOptions.Value;
    private readonly ILogger<FilteringSchedulerTask> _logger = logger;
    private readonly SemaphoreSlim _runLock = new(1, 1);

    public async Task ExecuteAsync()
    {
        if (!_filteringOptions.AllowConcurrentRuns)
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
            if (!_filteringOptions.AllowConcurrentRuns)
            {
                _runLock.Release();
            }
        }
    }

    private async Task ExecuteRunAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var filteringService = scope.ServiceProvider.GetRequiredService<IFilteringService>();

        var result = await filteringService.ExecuteAsync(CancellationToken.None);

        _logger.LogFilteringCompleted(
            result.Evaluated,
            result.ReadyForAnalysis,
            result.Removed,
            result.Deleted,
            result.Updated);
    }
}