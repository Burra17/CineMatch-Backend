using CineMatch.API.Common;
using CineMatch.Application.Features.WatchParties.Commands.CreateWatchParty;
using CineMatch.Application.Features.WatchParties.Commands.JoinWatchParty;
using CineMatch.Application.Features.WatchParties.Commands.LeaveWatchParty;
using CineMatch.Application.Features.WatchParties.Commands.StartWatchParty;
using CineMatch.Application.Features.WatchParties.Common.Dtos;
using CineMatch.Application.Features.WatchParties.Queries.GetWatchPartyDetails;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineMatch.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class WatchPartiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public WatchPartiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new WatchParty.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(WatchPartyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateWatchParty()
    {
        var result = await _mediator.Send(new CreateWatchPartyCommand());

        return result.ToActionResult(this);
    }

    /// <summary>
    /// Join an existing WatchParty using its join code.
    /// </summary>
    [HttpPost("join")]
    [ProducesResponseType(typeof(WatchPartyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> JoinWatchParty([FromBody] JoinWatchPartyCommand command)
    {
        var result = await _mediator.Send(command);

        return result.ToActionResult(this);
    }

    /// <summary>
    /// Leave a WatchParty. Soft-deletes the membership and closes the party if you are the last active member.
    /// </summary>
    [HttpPost("{id:guid}/leave")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> LeaveWatchParty([FromRoute] Guid id)
    {
        var result = await _mediator.Send(new LeaveWatchPartyCommand(id));

        return result.ToActionResult(this);
    }

    /// <summary>
    /// Start a WatchParty session. Only the host can call this.
    /// </summary>
    [HttpPost("{id:guid}/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartWatchParty([FromRoute] Guid id)
    {
        var result = await _mediator.Send(new StartWatchPartyCommand(id));

        return result.ToActionResult(this);
    }

    /// <summary>
    /// Get details for a WatchParty, including its member list. Only active members can access.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WatchPartyDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWatchPartyDetails([FromRoute] Guid id)
    {
        var result = await _mediator.Send(new GetWatchPartyDetailsQuery(id));

        return result.ToActionResult(this);
    }
}
