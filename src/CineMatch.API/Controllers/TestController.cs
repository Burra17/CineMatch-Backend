using CineMatch.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CineMatch.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IJwtService _jwtService;

        public TestController(IJwtService jwtService)
        {
            _jwtService = jwtService;
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
    }
}
