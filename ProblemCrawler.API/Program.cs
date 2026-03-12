using ProblemCrawler.Collectors.Reddit.Extensions;
using ProblemCrawler.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddLogging();

builder.Services.AddRedditCollector(builder.Configuration.GetSection("Collectors:Reddit"));
builder.Services.AddInfrastructure(builder.Configuration.GetSection("DatabaseSettings"));
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();

app.Run();
