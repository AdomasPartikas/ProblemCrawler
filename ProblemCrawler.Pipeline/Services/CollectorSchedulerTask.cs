using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProblemCrawler.Core.Configuration;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Pipeline.Interfaces;
using Microsoft.Extensions.Logging;
using ProblemCrawler.Logging.LoggerMessages;

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
            return;

        foreach (var service in collectionServices)
        {
            var (total, _) = await service.CollectAsync(CancellationToken.None);
            _logger.LogScheduledCollectionCompleted(total);
        }
    }
}