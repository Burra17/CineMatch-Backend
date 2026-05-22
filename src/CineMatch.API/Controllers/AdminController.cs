using CineMatch.API.Common;
using CineMatch.Application.Features.Admin.Common.Dtos;
using CineMatch.Application.Features.Admin.Queries.GetAllWatchParties;
using CineMatch.Application.Features.Admin.Queries.GetUserCount;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineMatch.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Returns the total number of registered users.
    /// </summary>
    [HttpGet("users/count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserCount()
    {
        var result = await _mediator.Send(new GetUserCountQuery());
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Returns all watch parties with host info and active member count.
    /// </summary>
    [HttpGet("watchparties")]
    [ProducesResponseType(typeof(List<AdminWatchPartyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllWatchParties()
    {
        var result = await _mediator.Send(new GetAllWatchPartiesQuery());
        return result.ToActionResult(this);
    }
}
