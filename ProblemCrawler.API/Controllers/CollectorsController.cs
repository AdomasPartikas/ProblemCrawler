using Microsoft.AspNetCore.Mvc;

using ProblemCrawler.Collectors.Reddit.Services;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Core.Records.Reddit;

namespace ProblemCrawler.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CollectorsController(ICollectionService collectionService) : ControllerBase
{
    private readonly ICollectionService _collectionService = collectionService ?? throw new ArgumentNullException(nameof(collectionService));

    [HttpGet("reddit/test")]
    [ProducesResponseType(typeof(RedditCollectionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RedditCollectionResponse>> TestRedditCollector(CancellationToken cancellationToken)
    {

        var (total, items) = await _collectionService.CollectAsync(cancellationToken);
        

        return Ok(new RedditCollectionResponse(
            "Reddit collection completed.",
            total,
            items));
    }
}

