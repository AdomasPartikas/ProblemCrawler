using Microsoft.Extensions.Hosting;
using Serilog;

namespace ProblemCrawler.Logging.Extensions;

public static class HostBuilderExtensions
{
    public static TBuilder AddProblemCrawlerLogging<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSerilog((services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext();
        });

        return builder;
    }
}