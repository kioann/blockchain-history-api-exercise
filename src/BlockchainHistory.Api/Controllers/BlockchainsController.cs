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

/// <summary>Capture snapshots and read history.</summary>
[ApiController]
[Route("api/v1/blockchains")]
public sealed class BlockchainsController(ISender sender) : ControllerBase
{
    /// <summary>Lists the supported chains.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BlockchainResponse>>(StatusCodes.Status200OK)]
    public IReadOnlyList<BlockchainResponse> List([FromServices] IOptions<BlockCypherOptions> blockCypher)
    {
        ArgumentNullException.ThrowIfNull(blockCypher);
        return BlockchainNetworks.All
            .Select(n => new BlockchainResponse(n.Slug, n.Coin, n.Chain, n.Model.ToString(), blockCypher.Value.SourceUrl(n)))
            .ToList();
    }

    /// <summary>Captures one chain now.</summary>
    /// <param name="network">eth-main, dash-main, btc-main, btc-test3 or ltc-main (any case).</param>
    /// <response code="201">Saved. Location links to it.</response>
    /// <response code="400">Unknown chain.</response>
    /// <response code="429">Hourly limit reached.</response>
    /// <response code="503">BlockCypher is unavailable.</response>
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

    /// <summary>Captures all chains at once.</summary>
    /// <response code="200">What was saved and what failed.</response>
    /// <response code="429">Hourly limit reached.</response>
    /// <response code="503">Nothing could be captured.</response>
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

    /// <summary>A chain's history, newest first.</summary>
    /// <param name="network">eth-main, dash-main, btc-main, btc-test3 or ltc-main (any case).</param>
    /// <param name="paging">Page (from 1) and page size (1 to 100).</param>
    /// <response code="200">One page of history.</response>
    /// <response code="400">Unknown chain or bad paging.</response>
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

    // Already validated by [SupportedBlockchain].
    private static BlockchainNetwork Parse(string slug) =>
        BlockchainNetworks.TryParse(slug, out var network)
            ? network
            : throw new ArgumentOutOfRangeException(nameof(slug), slug, "Unsupported blockchain.");
}
