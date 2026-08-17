using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace ProblemCrawler.Logging.Configuration;

public static class LoggingHostBuilderExtensions
{
    public static IHostBuilder UseProblemCrawlerLogging(this IHostBuilder hostBuilder, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentNullException.ThrowIfNull(configuration);

        return hostBuilder.UseSerilog((_, loggerConfiguration) =>
        {
            var serilogSection = configuration.GetSection("Serilog");
            if (serilogSection.Exists())
            {
                loggerConfiguration.ReadFrom.Configuration(configuration);
            }
            else
            {
                loggerConfiguration
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("System", LogEventLevel.Warning)
                    .MinimumLevel.Override("Hangfire", LogEventLevel.Error)
                    .WriteTo.Console()
                    .WriteTo.File(
                        path: "logs/problemcrawler-.log",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7);
            }

            loggerConfiguration.Enrich.FromLogContext();
        });
    }
}
