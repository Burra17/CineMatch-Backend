using CineMatch.API.Common;
using CineMatch.Application.Features.Users.Commands.LoginUser;
using CineMatch.Application.Features.Users.Commands.RegisterUser;
using CineMatch.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CineMatch.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IJwtService _jwtService;
        private readonly IMediator _mediator;

        public TestController(IJwtService jwtService, IMediator mediator)
        {
            _jwtService = jwtService;
            _mediator = mediator;
        }
        [HttpGet]
        public IActionResult GenerateTestToken()
        {
            var fakeUser = new Domain.Models.User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = "fakehash",

            };

            var token = _jwtService.GenerateToken(fakeUser);
            return Ok(new { token = token,
                decodeAt = "https://jwt.io/",
            user = new { fakeUser.Id, fakeUser.Username, fakeUser.Email, fakeUser.Role }
            });
        }

        [HttpPost("register-test")]
        public async Task<IActionResult> RegisterTest([FromBody] RegisterUserCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult(this);
        }

        [HttpPost("login-test")]
        public async Task<IActionResult> LoginTest([FromBody] LoginUserCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult(this);
        }
    }
}
