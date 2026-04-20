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
            .GetRequiredService<IOptions<FilteringSchedulingConfiguration>>()
            .Value;

        var llmAnalysisOptions = app.Services
            .GetRequiredService<IOptions<LLMAnalysisSchedulingConfiguration>>()
            .Value;

        var threadSynthesisOptions = app.Services
            .GetRequiredService<IOptions<ThreadSynthesisSchedulingConfiguration>>()
            .Value;
        var embeddingOptions = app.Services
            .GetRequiredService<IOptions<EmbeddingSchedulingConfiguration>>()
            .Value;
        var clusteringOptions = app.Services
            .GetRequiredService<IOptions<ClusteringSchedulingConfiguration>>()
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

        if (llmAnalysisOptions.Enabled)
        {
            RecurringJob.AddOrUpdate<ILLMAnalysisSchedulerTask>(
                recurringJobId: "llm-analysis:run-all",
                methodCall: static job => job.ExecuteAsync(),
                cronExpression: llmAnalysisOptions.CronExpression,
                options: new RecurringJobOptions
                {
                    TimeZone = TimeZoneResolver.Resolve(llmAnalysisOptions.TimeZoneId)
                });

            if (llmAnalysisOptions.RunOnStartup)
            {
                BackgroundJob.Enqueue<ILLMAnalysisSchedulerTask>(static job => job.ExecuteAsync());
            }
        }

        if (threadSynthesisOptions.Enabled)
        {
            RecurringJob.AddOrUpdate<IThreadSynthesisSchedulerTask>(
                recurringJobId: "thread-synthesis:run-all",
                methodCall: static job => job.ExecuteAsync(),
                cronExpression: threadSynthesisOptions.CronExpression,
                options: new RecurringJobOptions
                {
                    TimeZone = TimeZoneResolver.Resolve(threadSynthesisOptions.TimeZoneId)
                });

            if (threadSynthesisOptions.RunOnStartup)
            {
                BackgroundJob.Enqueue<IThreadSynthesisSchedulerTask>(static job => job.ExecuteAsync());
            }
        }

        if (embeddingOptions.Enabled)
        {
            RecurringJob.AddOrUpdate<IIdeaEmbeddingSchedulerTask>(
                recurringJobId: "idea-embedding:run-all",
                methodCall: static job => job.ExecuteAsync(),
                cronExpression: embeddingOptions.CronExpression,
                options: new RecurringJobOptions
                {
                    TimeZone = TimeZoneResolver.Resolve(embeddingOptions.TimeZoneId)
                });

            if (embeddingOptions.RunOnStartup)
            {
                BackgroundJob.Enqueue<IIdeaEmbeddingSchedulerTask>(static job => job.ExecuteAsync());
            }
        }

        if (clusteringOptions.Enabled)
        {
            RecurringJob.AddOrUpdate<IClusteringSchedulerTask>(
                recurringJobId: "clustering:run",
                methodCall: static job => job.ExecuteAsync(),
                cronExpression: clusteringOptions.CronExpression,
                options: new RecurringJobOptions
                {
                    TimeZone = TimeZoneResolver.Resolve(clusteringOptions.TimeZoneId)
                });

            if (clusteringOptions.RunOnStartup)
            {
                BackgroundJob.Enqueue<IClusteringSchedulerTask>(static job => job.ExecuteAsync());
            }
        }

        return app;
    }
}