using CineMatch.API.Common;
using CineMatch.Application.Features.Users.Commands.LoginUser;
using CineMatch.Application.Features.Users.Commands.RegisterUser;
using CineMatch.Application.Features.Users.Common.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CineMatch.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterUser([FromBody] RegisterUserCommand command)
    {
        var result = await _mediator.Send(command);

        return result.ToActionResult(this);
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginUser([FromBody] LoginUserCommand command)
    {
        var result = await _mediator.Send(command);

        return result.ToActionResult(this);
    }
}
