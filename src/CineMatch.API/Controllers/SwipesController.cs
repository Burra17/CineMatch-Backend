using CineMatch.API.Common;
using CineMatch.Application.Features.Swipes.Commands.CreateSwipe;
using CineMatch.Application.Features.Swipes.Common.Dtos;
using CineMatch.Application.Features.Swipes.Queries.GetSwipeQueue;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineMatch.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SwipesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SwipesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Register a swipe (like or dislike) on a movie in a WatchParty.
    /// Returns whether a match was created and, if so, the matched movie.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SwipeResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateSwipe([FromBody] CreateSwipeCommand command)
    {
        var result = await _mediator.Send(command);

        return result.ToActionResult(this);
    }

    /// <summary>
    /// Get the next movies to swipe on for the requesting user in a WatchParty.
    /// Excludes movies the user has already swiped on. Returns up to <paramref name="count"/> movies in queue order.
    /// </summary>
    [HttpGet("queue/{watchPartyId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<MovieDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSwipeQueue([FromRoute] Guid watchPartyId, [FromQuery] int count = 10)
    {
        var result = await _mediator.Send(new GetSwipeQueueQuery(watchPartyId, count));

        return result.ToActionResult(this);
    }
}
