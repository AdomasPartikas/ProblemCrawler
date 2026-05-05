using Microsoft.Extensions.DependencyInjection;
using ProblemCrawler.API.Helpers;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Pipeline.Interfaces;
using ProblemCrawler.Pipeline.Services;

namespace ProblemCrawler.Pipeline.Extensions;

/// <summary>
/// Extension methods for registering pipeline services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the collection pipeline services, including <see cref="ICollectionService"/>.
    /// </summary>
    public static IServiceCollection AddCollectionPipeline(this IServiceCollection services)
    {
        services.AddScoped<ICollectionService, CollectionService>();
        return services;
    }

    /// <summary>
    /// Registers the filtering pipeline service.
    /// </summary>
    public static IServiceCollection AddFilteringPipeline(this IServiceCollection services)
    {
        services.AddScoped<IFilteringService, FilteringService>();
        return services;
    }

    /// <summary>
    /// Registers the LLM analysis pipeline service.
    /// </summary>
    public static IServiceCollection AddLLMAnalysisPipeline(this IServiceCollection services)
    {
        services.AddScoped<ILLMAnalysisService, LLMAnalysisService>();
        return services;
    }

    /// <summary>
    /// Registers the thread synthesis pipeline service.
    /// </summary>
    public static IServiceCollection AddThreadSynthesisPipeline(this IServiceCollection services)
    {
        services.AddScoped<IThreadSynthesisService, ThreadSynthesisService>();
        return services;
    }

    /// <summary>
    /// Registers the embedding pipeline service.
    /// </summary>
    public static IServiceCollection AddEmbeddingPipeline(this IServiceCollection services)
    {
        services.AddScoped<IIdeaEmbeddingService, IdeaEmbeddingService>();
        return services;
    }
    /// <summary>
    /// Registers the clustering pipeline service.
    /// </summary>
    public static IServiceCollection AddClusteringPipeline(this IServiceCollection services)
    {
        services.AddSingleton<IClusteringSchedulerTask, ClusteringSchedulerTask>();
        services.AddScoped<IClusterJobRunner, ClusterJobRunner>();
        return services;
    }


}
