using Hangfire;
using ProblemCrawler.API.Extensions;
using ProblemCrawler.Collectors.Reddit.Extensions;
using ProblemCrawler.Infrastructure;
using ProblemCrawler.Pipeline.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddLogging();
builder.Services.AddCollectorScheduling(builder.Configuration);

builder.Services.AddRedditCollector(builder.Configuration.GetSection("Collectors:Reddit"));
builder.Services.AddInfrastructure(builder.Configuration.GetSection("DatabaseSettings"));
builder.Services.AddCollectionPipeline();
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
