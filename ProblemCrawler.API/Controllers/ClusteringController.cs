using Microsoft.AspNetCore.Mvc;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Core.Records.Clustering;
using ProblemCrawler.Pipeline.Interfaces;

namespace ProblemCrawler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class ClusteringController(
       IClusterRepository clusterRepository) : ControllerBase
    {
        private readonly IClusterRepository _clusterRepository = clusterRepository;

        /// <summary>
        /// Retrieves a list of all cluster runs.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>An <see cref="ActionResult{T}"/> containing a read-only list of <see cref="ClusterRun"/> objects
        /// representing all cluster runs. Returns status code 200 (OK) with the list of runs.</returns>
        [HttpGet("runs")]
        [ProducesResponseType(typeof(IReadOnlyList<ClusterRun>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<ClusterRun>>> GetRuns(CancellationToken cancellationToken)
        {
            var runs = await _clusterRepository.GetAllRunsAsync(cancellationToken);
            return Ok(runs);
        }
        /// <summary>
        /// Retrieves the list of problem clusters associated with the specified run.
        /// </summary>
        /// <param name="runId">The unique identifier of the run for which to retrieve problem clusters.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
        /// <returns>An <see cref="ActionResult{T}"/> containing a read-only list of <see cref="ProblemCluster"/> objects if any
        /// are found; otherwise, a 404 Not Found response if no clusters exist for the specified run.</returns>
        [HttpGet("runs/{runId:guid}/clusters")]
        [ProducesResponseType(typeof(IReadOnlyList<ProblemCluster>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IReadOnlyList<ProblemCluster>>> GetClusters(Guid runId, CancellationToken cancellationToken)
        {
            var clusters = await _clusterRepository.GetClustersForRunAsync(runId, cancellationToken);
            if (clusters.Count == 0) return NotFound();
            return Ok(clusters);
        }
        /// <summary>
        /// Retrieves a list of idea summaries for the specified cluster within a run.
        /// </summary>
        /// <param name="runId">The unique identifier of the run containing the cluster.</param>
        /// <param name="clusterId">The identifier of the cluster for which to retrieve idea summaries.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An action result containing a read-only list of idea summaries for the specified cluster. Returns a 404 Not
        /// Found response if no ideas are found.</returns>
        [HttpGet("runs/{runId:guid}/clusters/{clusterId:int}/ideas")]
        [ProducesResponseType(typeof(IReadOnlyList<ClusterIdeaSummary>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IReadOnlyList<ClusterIdeaSummary>>> GetIdeas(Guid runId, int clusterId, CancellationToken cancellationToken)
        {
            var ideas = await _clusterRepository.GetIdeasForClusterAsync(runId, clusterId, cancellationToken);
            if (ideas.Count == 0) return NotFound();
            return Ok(ideas);
        }
        /// <summary>
        /// Pins or unpins a run with the specified identifier.
        /// </summary>
        /// <param name="runId">The unique identifier of the run to pin or unpin.</param>
        /// <param name="pinned">A value indicating whether the run should be pinned. Set to <see langword="true"/> to pin the run;
        /// otherwise, <see langword="false"/>.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="NoContentResult"/> if the operation succeeds; otherwise, a <see cref="NotFoundResult"/> if the
        /// run does not exist.</returns>
        [HttpPatch("runs/{runId:guid}/pin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PinRun(Guid runId, [FromQuery] bool pinned, CancellationToken cancellationToken)
        {
            var found = await _clusterRepository.SetPinnedAsync(runId, pinned, cancellationToken);
            if (!found) return NotFound();
            return NoContent();
        }
    }
}
