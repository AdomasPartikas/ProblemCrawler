using Hangfire;
using Hangfire.InMemory;
using ProblemCrawler.Core.Configuration;
using ProblemCrawler.Pipeline.Interfaces;
using ProblemCrawler.Pipeline.Services;

namespace ProblemCrawler.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCollectorScheduling(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CollectorSchedulingConfiguration>(
            configuration.GetSection("Collectors:Scheduling"));

        services.AddSingleton<ICollectorSchedulerTask, CollectorSchedulerTask>();

        services.AddHangfire(static config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseInMemoryStorage());

        services.AddHangfireServer();

        return services;
    }
}