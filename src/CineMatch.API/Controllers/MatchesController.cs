using CineMatch.API.Common;
using CineMatch.Application.Features.Matches.Commands.MarkMatchAsWatched;
using CineMatch.Application.Features.Matches.Common.Dtos;
using CineMatch.Application.Features.Matches.Queries.GetMatchesByParty;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineMatch.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class MatchesController : ControllerBase
{
    private readonly IMediator _mediator;

    public MatchesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all matches for a WatchParty, ordered by most recent first.
    /// Only active members of the party can access this endpoint.
    /// </summary>
    [HttpGet("party/{watchPartyId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<MatchDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMatchesByParty([FromRoute] Guid watchPartyId)
    {
        var result = await _mediator.Send(new GetMatchesByPartyQuery(watchPartyId));

        return result.ToActionResult(this);
    }

    /// <summary>
    /// Mark a matched movie as watched.
    /// Only active members of the party that owns the match can call this endpoint.
    /// </summary>
    [HttpPost("{matchId:guid}/watched")]
    [ProducesResponseType(typeof(MatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MarkMatchAsWatched([FromRoute] Guid matchId)
    {
        var result = await _mediator.Send(new MarkMatchAsWatchedCommand(matchId));

        return result.ToActionResult(this);
    }
}
