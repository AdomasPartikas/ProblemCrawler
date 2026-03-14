using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProblemCrawler.Core.Configuration;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Pipeline.Interfaces;

namespace ProblemCrawler.Pipeline.Services;

/// <summary>
/// Runs all registered collectors for a scheduled job execution.
/// </summary>
public sealed class CollectorSchedulerTask(
    IServiceScopeFactory scopeFactory,
    IOptions<CollectorSchedulingConfiguration> schedulingOptions) : ICollectorSchedulerTask
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly CollectorSchedulingConfiguration _schedulingOptions = schedulingOptions.Value;
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

        var collectors = scope.ServiceProvider.GetServices<ICollector>().ToArray();

        if (collectors.Length == 0)
            return;

        foreach (var collector in collectors)
            await foreach (var _ in collector.GatherAsync());
    }
}