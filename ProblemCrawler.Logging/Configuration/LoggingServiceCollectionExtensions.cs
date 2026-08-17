using Microsoft.Extensions.DependencyInjection;

namespace ProblemCrawler.Logging.Configuration;

public static class LoggingServiceCollectionExtensions
{
    public static IServiceCollection AddProblemCrawlerLogging(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
