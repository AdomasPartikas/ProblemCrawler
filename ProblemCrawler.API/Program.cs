var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    Name = "ProblemCrawler",
    Status = "running",
    Timestamp = DateTimeOffset.UtcNow
}));

app.Run();
