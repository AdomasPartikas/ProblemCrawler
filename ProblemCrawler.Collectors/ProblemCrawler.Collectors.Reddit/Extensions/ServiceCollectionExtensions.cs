using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ProblemCrawler.Core.Configuration;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Collectors.Reddit.Services;
using ProblemCrawler.Collectors.Reddit.Profiles;

namespace ProblemCrawler.Collectors.Reddit.Extensions;

/// <summary>
/// Extension methods for registering the Reddit collector in the dependency injection container.
/// </summary>
public static class RedditCollectorServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Reddit collector and its dependencies.
    /// </summary>
    public static IServiceCollection AddRedditCollector(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        services.Configure<RedditCollectorConfiguration>(configurationSection);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<RedditCollectorConfiguration>>().Value);
        services.AddAutoMapper(typeof(RedditCollectorMappingProfile).Assembly);

        services.AddHttpClient<RedditHttpClient>((sp, client) =>
        {
            var config = sp.GetRequiredService<IOptions<RedditCollectorConfiguration>>().Value;
            client.DefaultRequestHeaders.UserAgent.ParseAdd(config.UserAgent);
            client.Timeout = TimeSpan.FromMilliseconds(config.RequestTimeoutMs);
        });

        services.AddScoped<ICollector, RedditCollector>();
        services.AddScoped<ICollectionService, CollectionService>();
        return services;
    }
}
