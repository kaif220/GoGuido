using Microsoft.AspNetCore.Mvc;
using GuidoApi.DTOs;
using GuidoApi.Services;


namespace GuidoApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterDto dto)
        {
            _authService.Register(dto);
            return Ok("User registered successfully");
        }
    }
}
