using Microsoft.Extensions.DependencyInjection;
using ProblemCrawler.Core.Interfaces;
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
}
