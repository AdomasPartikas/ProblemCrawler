using ProblemCrawler.Collectors.Reddit.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddLogging();

builder.Services.AddRedditCollector(builder.Configuration.GetSection("Collectors:Reddit"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
