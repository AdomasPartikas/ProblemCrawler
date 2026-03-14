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
        var schedulingOptions = app.Services
            .GetRequiredService<IOptions<CollectorSchedulingConfiguration>>()
            .Value;

        if (!schedulingOptions.Enabled)
        {
            return app;
        }

        RecurringJob.AddOrUpdate<ICollectorSchedulerTask>(
            recurringJobId: "collectors:run-all",
            methodCall: static job => job.ExecuteAsync(),
            cronExpression: schedulingOptions.CronExpression,
            options: new RecurringJobOptions
            {
                TimeZone = TimeZoneResolver.Resolve(schedulingOptions.TimeZoneId)
            });

        if (schedulingOptions.RunOnStartup)
        {
            BackgroundJob.Enqueue<ICollectorSchedulerTask>(static job => job.ExecuteAsync());
        }

        return app;
    }
}