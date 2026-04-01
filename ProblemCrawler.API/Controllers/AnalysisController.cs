using Microsoft.AspNetCore.Mvc;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Core.Records.LLM;

namespace ProblemCrawler.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AnalysisController(
    ILLMAnalysisService llmAnalysisService,
    IThreadSynthesisService threadSynthesisService) : ControllerBase
{
    private readonly ILLMAnalysisService _llmAnalysisService = llmAnalysisService;
    private readonly IThreadSynthesisService _threadSynthesisService = threadSynthesisService;

    [HttpPost("llm/test/{collectorItemId:guid}")]
    [ProducesResponseType(typeof(LLMAnalysisExecutionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LLMAnalysisExecutionResult>> TestSingleItem(Guid collectorItemId, CancellationToken cancellationToken)
    {
        var result = await _llmAnalysisService.ExecuteForItemAsync(collectorItemId, cancellationToken);
        if (!result.Success && string.Equals(result.Message, "Collector item was not found.", StringComparison.Ordinal))
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost("thread-synthesis/test/{rootCollectorItemId:guid}")]
    [ProducesResponseType(typeof(ThreadSynthesisExecutionResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<ThreadSynthesisExecutionResult>> TestThreadSynthesis(Guid rootCollectorItemId, CancellationToken cancellationToken)
    {
        var result = await _threadSynthesisService.ExecuteForThreadAsync(rootCollectorItemId, cancellationToken);
        return Ok(result);
    }
}
