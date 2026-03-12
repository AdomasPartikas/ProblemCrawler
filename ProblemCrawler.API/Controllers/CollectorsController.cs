using Microsoft.AspNetCore.Mvc;
using ProblemCrawler.Core.Interfaces;

namespace ProblemCrawler.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CollectorsController(ICollector collector, ILogger<CollectorsController> logger) : ControllerBase
{
    private readonly ICollector _collector = collector;
    private readonly ILogger<CollectorsController> _logger = logger;

    [HttpGet("reddit/test")]
    [ProducesResponseType(typeof(RedditCollectionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RedditCollectionResponse>> TestRedditCollector(CancellationToken cancellationToken)
    {
        var items = new List<CollectedItemResponse>();

        await foreach (var item in _collector.GatherAsync(cancellationToken))
        {
            _logger.LogInformation(
                "Collected {ItemType} {ItemId} from {Source} by {Author}",
                item.ItemType,
                item.Id,
                item.Source,
                item.Author);

            items.Add(new CollectedItemResponse(
                item.Id,
                item.ItemType,
                item.Author,
                item.CreatedAt,
                item.SourceUrl));
        }

        return Ok(new RedditCollectionResponse(
            "Reddit collection completed.",
            items.Count,
            items));
    }
}

public sealed record RedditCollectionResponse(
    string Message,
    int TotalItems,
    IReadOnlyList<CollectedItemResponse> Items);

public sealed record CollectedItemResponse(
    string Id,
    string ItemType,
    string? Author,
    DateTime CreatedAt,
    string? SourceUrl);