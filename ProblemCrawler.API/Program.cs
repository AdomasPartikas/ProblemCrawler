using Hangfire;
using ProblemCrawler.API.Extensions;
using ProblemCrawler.API.Helpers;
using ProblemCrawler.Collectors.Reddit.Extensions;
using ProblemCrawler.Infrastructure;
using ProblemCrawler.Logging.Configuration;
using ProblemCrawler.Pipeline.Extensions;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseProblemCrawlerLogging(builder.Configuration);
    builder.Services.AddProblemCrawlerLogging();

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddCollectorScheduling(builder.Configuration);

    builder.Services.AddRedditCollector(builder.Configuration.GetSection("Collectors:Reddit"));
    builder.Services.AddInfrastructure(builder.Configuration.GetSection("DatabaseSettings"));
    builder.Services.AddCollectionPipeline();
    builder.Services.AddFilteringPipeline();
    builder.Services.AddLLMAnalysisPipeline();
    builder.Services.AddThreadSynthesisPipeline();
    builder.Services.AddEmbeddingPipeline();
    builder.Services.AddClusteringPipeline();
    builder.Services.AddHostedService<OllamaStartupService>();
    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
        app.MapHangfireDashboard("/jobs");
    }

    app.UseCollectorScheduling();

    app.MapControllers();

    app.MapGet("/", () => Results.Redirect("/scalar"))
       .ExcludeFromDescription();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
