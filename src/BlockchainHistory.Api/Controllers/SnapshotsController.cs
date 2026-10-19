using BlockchainHistory.Application.Snapshots;
using BlockchainHistory.Application.Snapshots.GetById;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace BlockchainHistory.Api.Controllers;

/// <summary>Single snapshots.</summary>
[ApiController]
[Route("api/v1/snapshots")]
public sealed class SnapshotsController(ISender sender) : ControllerBase
{
    public const string GetByIdRoute = "GetSnapshotById";

    /// <summary>Gets one snapshot.</summary>
    /// <param name="id">The snapshot id.</param>
    /// <response code="200">The snapshot.</response>
    /// <response code="404">Not found.</response>
    [HttpGet("{id:guid}", Name = GetByIdRoute)]
    [ProducesResponseType<SnapshotDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SnapshotDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var snapshot = await sender.Send(new GetSnapshotByIdQuery(id), cancellationToken);
        return snapshot is null ? NotFound() : snapshot;
    }
}
