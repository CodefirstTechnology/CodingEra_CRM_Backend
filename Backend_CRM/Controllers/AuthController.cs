using CRM.Business.Services;
using CRM.DTO;
using CRM.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromQuery] int? userId, [FromBody] RegisterRequest req) =>
        (await authService.RegisterAsync(userId, req)).ToActionResult();

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req) =>
        (await authService.LoginAsync(req)).ToActionResult();

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers([FromQuery] int userId)
    {
        _ = userId;
        return Ok(await authService.GetAllUsersAsync());
    }

    [HttpGet("users/{id:int}")]
    public async Task<IActionResult> GetUser(int id, [FromQuery] int userId)
    {
        _ = userId;
        return (await authService.GetUserAsync(id)).ToActionResult();
    }
}
