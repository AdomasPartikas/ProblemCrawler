using Hangfire;
using Microsoft.Extensions.Options;
using ProblemCrawler.API.Helpers;
using ProblemCrawler.Core.Configuration;
using ProblemCrawler.Pipeline.Interfaces;

namespace ProblemCrawler.API.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseCollectorScheduling(this WebApplication app)
    {
        var collectorOptions = app.Services
            .GetRequiredService<IOptions<CollectorSchedulingConfiguration>>()
            .Value;

        var filteringOptions = app.Services
            .GetRequiredService<IOptions<FilteringConfiguration>>()
            .Value;

        if (collectorOptions.Enabled)
        {
            RecurringJob.AddOrUpdate<ICollectorSchedulerTask>(
                recurringJobId: "collectors:run-all",
                methodCall: static job => job.ExecuteAsync(),
                cronExpression: collectorOptions.CronExpression,
                options: new RecurringJobOptions
                {
                    TimeZone = TimeZoneResolver.Resolve(collectorOptions.TimeZoneId)
                });

            if (collectorOptions.RunOnStartup)
            {
                BackgroundJob.Enqueue<ICollectorSchedulerTask>(static job => job.ExecuteAsync());
            }
        }

        if (filteringOptions.Enabled)
        {
            RecurringJob.AddOrUpdate<IFilteringSchedulerTask>(
                recurringJobId: "filtering:run-all",
                methodCall: static job => job.ExecuteAsync(),
                cronExpression: filteringOptions.CronExpression,
                options: new RecurringJobOptions
                {
                    TimeZone = TimeZoneResolver.Resolve(filteringOptions.TimeZoneId)
                });

            if (filteringOptions.RunOnStartup)
            {
                BackgroundJob.Enqueue<IFilteringSchedulerTask>(static job => job.ExecuteAsync());
            }
        }

        return app;
    }
}