using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Pipeline.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Pipeline.Services
{
    public sealed class ClusteringSchedulerTask(
       IServiceScopeFactory scopeFactory,
       ILogger<ClusteringSchedulerTask> logger) : IClusteringSchedulerTask
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly ILogger<ClusteringSchedulerTask> _logger = logger;

        public async Task ExecuteAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var runner = scope.ServiceProvider.GetRequiredService<IClusterJobRunner>();

            _logger.LogInformation("[cluster] Scheduler triggering cluster job");
            await runner.RunAsync(CancellationToken.None);
            _logger.LogInformation("[cluster] Scheduler cluster job completed");
        }
    }
}
