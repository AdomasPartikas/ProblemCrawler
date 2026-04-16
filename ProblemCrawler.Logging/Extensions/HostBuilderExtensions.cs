using Microsoft.Extensions.Hosting;
using Serilog;

namespace ProblemCrawler.Logging.Extensions;

public static class HostBuilderExtensions
{
    private const string ConsoleOutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}";

    public static TBuilder AddProblemCrawlerLogging<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSerilog((services, loggerConfiguration) =>
        {
            loggerConfiguration
                .MinimumLevel.Verbose()
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: ConsoleOutputTemplate);
        });

        return builder;
    }
}