using BlockchainHistory.Api.Contracts;
using BlockchainHistory.Api.Errors;
using BlockchainHistory.Api.Extensions;
using BlockchainHistory.Application.Common;
using BlockchainHistory.Application.Snapshots;
using BlockchainHistory.Application.Snapshots.Capture;
using BlockchainHistory.Application.Snapshots.CaptureAll;
using BlockchainHistory.Application.Snapshots.GetHistory;
using BlockchainHistory.Domain.Chains;
using BlockchainHistory.Infrastructure.BlockCypher;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace BlockchainHistory.Api.Controllers;

/// <summary>Capture blockchain snapshots and read each chain's history.</summary>
[ApiController]
[Route("api/v1/blockchains")]
public sealed class BlockchainsController(ISender sender) : ControllerBase
{
    /// <summary>Lists the supported blockchains.</summary>
    /// <param name="blockCypher">Provides the configured BlockCypher base URL.</param>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BlockchainResponse>>(StatusCodes.Status200OK)]
    public IReadOnlyList<BlockchainResponse> List([FromServices] IOptions<BlockCypherOptions> blockCypher)
    {
        ArgumentNullException.ThrowIfNull(blockCypher);
        return BlockchainNetworks.All
            .Select(n => new BlockchainResponse(n.Slug, n.Coin, n.Chain, n.Model.ToString(), blockCypher.Value.SourceUrl(n)))
            .ToList();
    }

    /// <summary>Fetches one blockchain from BlockCypher now and stores it as a new snapshot.</summary>
    /// <param name="network">eth-main, dash-main, btc-main, btc-test3 or ltc-main (case-insensitive).</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="201">The stored snapshot. Location points to it.</response>
    /// <response code="400">Unknown blockchain.</response>
    /// <response code="429">The hourly capture budget is used up.</response>
    /// <response code="503">BlockCypher could not be reached.</response>
    [HttpPost("{network}/snapshots")]
    [EnableRateLimiting(RateLimitingExtensions.CapturePolicy)]
    [ProducesResponseType<SnapshotDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<SnapshotDto>> Capture(
        [FromRoute, SupportedBlockchain] string network, CancellationToken cancellationToken)
    {
        var snapshot = await sender.Send(new CaptureSnapshotCommand(Parse(network)), cancellationToken);
        return CreatedAtRoute(SnapshotsController.GetByIdRoute, new { id = snapshot.Id }, snapshot);
    }

    /// <summary>Fetches every supported blockchain concurrently and stores those that succeed.</summary>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">At least one network was stored. Lists what was stored, and which networks failed and why.</response>
    /// <response code="429">The hourly capture budget is used up.</response>
    /// <response code="503">No network could be fetched from BlockCypher.</response>
    [HttpPost("snapshots")]
    [EnableRateLimiting(RateLimitingExtensions.CapturePolicy)]
    [ProducesResponseType<CaptureAllResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<CaptureAllResult>> CaptureAll(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CaptureAllSnapshotsCommand(), cancellationToken);
        if (result.Captured.Count > 0)
        {
            return result;
        }

        return Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: GlobalExceptionHandler.UpstreamUnavailableTitle,
            detail: "BlockCypher could not provide data for any network. Try again later.");
    }

    /// <summary>Returns a blockchain's stored snapshots, newest CreatedAt first.</summary>
    /// <param name="network">eth-main, dash-main, btc-main, btc-test3 or ltc-main (case-insensitive).</param>
    /// <param name="paging">Page (from 1) and page size (1 to 100).</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">One page of history.</response>
    /// <response code="400">Unknown blockchain or invalid paging.</response>
    [HttpGet("{network}/snapshots")]
    [ProducesResponseType<PagedResult<SnapshotDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<PagedResult<SnapshotDto>> History(
        [FromRoute, SupportedBlockchain] string network,
        [FromQuery] HistoryQueryParameters paging,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(paging);
        return await sender.Send(
            new GetSnapshotHistoryQuery(Parse(network), paging.Page, paging.PageSize), cancellationToken);
    }

    // Safe after [SupportedBlockchain] has validated the value.
    private static BlockchainNetwork Parse(string slug) =>
        BlockchainNetworks.TryParse(slug, out var network)
            ? network
            : throw new ArgumentOutOfRangeException(nameof(slug), slug, "Unsupported blockchain.");
}
