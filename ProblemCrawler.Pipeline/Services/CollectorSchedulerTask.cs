using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProblemCrawler.Core.Configuration;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Logging.LogMessages;
using ProblemCrawler.Pipeline.Interfaces;
using Microsoft.Extensions.Logging;

namespace ProblemCrawler.Pipeline.Services;

/// <summary>
/// Runs all registered collectors for a scheduled job execution.
/// </summary>
public sealed class CollectorSchedulerTask(
    IServiceScopeFactory scopeFactory,
    IOptions<CollectorSchedulingConfiguration> schedulingOptions,
    ILogger<CollectorSchedulerTask> logger) : ICollectorSchedulerTask
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly CollectorSchedulingConfiguration _schedulingOptions = schedulingOptions.Value;
    private readonly ILogger<CollectorSchedulerTask> _logger = logger;
    private readonly SemaphoreSlim _runLock = new(1, 1);

    public async Task ExecuteAsync()
    {
        if (!_schedulingOptions.AllowConcurrentRuns)
        {
            var lockAcquired = await _runLock.WaitAsync(0);
            if (!lockAcquired)
            {
                _logger.LogCollectorSchedulerSkippedConcurrentRun();
                return;
            }
        }

        try
        {
            await ExecuteRunAsync();
        }
        finally
        {
            if (!_schedulingOptions.AllowConcurrentRuns)
            {
                _runLock.Release();
            }
        }
    }

    private async Task ExecuteRunAsync()
    {
        using var scope = _scopeFactory.CreateScope();

        var collectionServices = scope.ServiceProvider.GetServices<ICollectionService>().ToArray();

        if (collectionServices.Length == 0)
        {
            _logger.LogNoCollectionServicesRegistered();
            return;
        }

        _logger.LogCollectorSchedulerRunStarted(collectionServices.Length);

        var totalCollectedItems = 0;
        var serviceIndex = 0;

        foreach (var service in collectionServices)
        {
            serviceIndex++;
            _logger.LogCollectorServiceExecutionStarted(service.GetType().Name, serviceIndex, collectionServices.Length);

            var (total, _) = await service.CollectAsync(CancellationToken.None);
            totalCollectedItems += total;
            _logger.LogCollectorServiceExecutionCompleted(service.GetType().Name, total);
        }

        _logger.LogCollectorSchedulerRunCompleted(collectionServices.Length, totalCollectedItems);
    }
}