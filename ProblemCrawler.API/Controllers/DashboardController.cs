using Microsoft.AspNetCore.Mvc;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Core.Records.Clustering;

namespace ProblemCrawler.API.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public sealed class DashboardController(IClusterRepository clusterRepository) : ControllerBase
    {
        [HttpGet("runs")]
        [ProducesResponseType(typeof(IReadOnlyList<ClusterRun>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRuns(CancellationToken ct)
        {
            var runs = await clusterRepository.GetAllRunsAsync(ct);
            return Ok(runs);
        }

        [HttpGet("opportunities")]
        [ProducesResponseType(typeof(OpportunitiesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOpportunities(
            [FromQuery] Guid? clusterRunId,
            CancellationToken ct)
        {
            var runId = clusterRunId ?? await clusterRepository.GetLatestClusterRunIdAsync(ct);
            if (runId is null) return NotFound("No cluster runs found.");

            var scores = await clusterRepository.GetOpportunityScoresAsync(runId.Value, ct);
            return Ok(new OpportunitiesResponse(runId.Value, scores));
        }

        [HttpGet("atlas")]
        [ProducesResponseType(typeof(AtlasResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAtlas(
            [FromQuery] Guid? clusterRunId,
            CancellationToken ct)
        {
            var runId = clusterRunId ?? await clusterRepository.GetLatestClusterRunIdAsync(ct);
            if (runId is null) return NotFound("No cluster runs found.");

            var points = await clusterRepository.GetUmapPointsAsync(runId.Value, ct);
            return Ok(new AtlasResponse(runId.Value, points));
        }

        [HttpGet("clusters/{clusterId:int}")]
        [ProducesResponseType(typeof(ClusterDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetClusterDetail(
            int clusterId,
            [FromQuery] Guid? clusterRunId,
            CancellationToken ct)
        {
            var runId = clusterRunId ?? await clusterRepository.GetLatestClusterRunIdAsync(ct);
            if (runId is null) return NotFound("No cluster runs found.");

            var detail = await clusterRepository.GetClusterDetailAsync(runId.Value, clusterId, ct);
            if (detail is null) return NotFound($"Cluster {clusterId} not found in run {runId}.");

            return Ok(new ClusterDetailResponse(runId.Value, detail));
        }

        [HttpPost("clusters/{clusterId:int}/validation")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SubmitValidation(
            int clusterId,
            [FromQuery] Guid? clusterRunId,
            [FromBody] ClusterValidationInput input,
            CancellationToken ct)
        {
            if (input.CoherenceRating < 1 || input.CoherenceRating > 10 ||
                input.NoveltyRating < 1 || input.NoveltyRating > 10 ||
                input.ProductPotentialRating < 1 || input.ProductPotentialRating > 10)
                return BadRequest("Ratings must be between 1 and 10.");

            var runId = clusterRunId ?? await clusterRepository.GetLatestClusterRunIdAsync(ct);
            if (runId is null) return NotFound("No cluster runs found.");

            var found = await clusterRepository.SaveValidationAsync(runId.Value, clusterId, input, ct);
            if (!found) return NotFound($"Cluster {clusterId} not found in run {runId}.");

            return NoContent();
        }
    }
}