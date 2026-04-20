using Hangfire;
using Hangfire.InMemory;
using Microsoft.Extensions.Options;
using ProblemCrawler.API.Helpers;
using ProblemCrawler.Core.Configuration;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Pipeline.Clients;
using ProblemCrawler.Pipeline.Helper;
using ProblemCrawler.Pipeline.Interfaces;
using ProblemCrawler.Pipeline.Services;

namespace ProblemCrawler.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCollectorScheduling(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CollectorSchedulingConfiguration>(
            configuration.GetSection("Collectors:Scheduling"));

        services.Configure<FilteringSchedulingConfiguration>(
            configuration.GetSection("Filtering:Scheduling"));

        services.Configure<FilteringConfiguration>(
            configuration.GetSection("Filtering:Settings"));

        services.Configure<LLMAnalysisSchedulingConfiguration>(
            configuration.GetSection("LLMAnalysis:Scheduling"));

        services.Configure<LLMAnalysisConfiguration>(
            configuration.GetSection("LLMAnalysis:Settings"));

        services.Configure<ThreadSynthesisSchedulingConfiguration>(
            configuration.GetSection("ThreadSynthesis:Scheduling"));

        services.Configure<ThreadSynthesisConfiguration>(
            configuration.GetSection("ThreadSynthesis:Settings"));

        services.Configure<OllamaConfiguration>(
            configuration.GetSection("LLMAnalysis:Ollama"));

        services.Configure<EmbeddingSchedulingConfiguration>(
             configuration.GetSection("Embedding:Scheduling"));

        services.Configure<EmbeddingConfiguration>(
            configuration.GetSection("Embedding:Settings"));
        services.Configure<ClusteringSchedulingConfiguration>(
            configuration.GetSection("Clustering:Scheduling"));

        services.AddSingleton<IClusteringSchedulerTask, ClusteringSchedulerTask>();
        services.AddSingleton<ICollectorSchedulerTask, CollectorSchedulerTask>();
        services.AddSingleton<IFilteringSchedulerTask, FilteringSchedulerTask>();
        services.AddSingleton<ILLMAnalysisSchedulerTask, LLMAnalysisSchedulerTask>();
        services.AddSingleton<IThreadSynthesisSchedulerTask, ThreadSynthesisSchedulerTask>();
        services.AddSingleton<IIdeaEmbeddingSchedulerTask, IdeaEmbeddingSchedulerTask>();
        services.AddSingleton<IClusterJobRunner, ClusterJobRunner>();
        services.AddSingleton<OllamaJobGate>();
        services.AddHttpClient<OllamaHttpClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<OllamaConfiguration>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromMilliseconds(options.RequestTimeoutMs);
        });

        services.AddHangfire(static config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseInMemoryStorage());

        services.AddHangfireServer();

        return services;
    }
}