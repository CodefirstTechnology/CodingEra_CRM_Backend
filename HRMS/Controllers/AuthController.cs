using HRMS.DTOs;
using HRMS.Models;
using HRMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

[Route("api/auth")]
[ApiController]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email and password are required." });
        }

        var (response, error) = await _authService.LoginAsync(request, cancellationToken);
        return response == null ? Unauthorized(new { message = error }) : Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
            ?? User.FindFirst("role")?.Value;
        var employeeId = User.FindFirst("employeeId")?.Value;

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(role))
        {
            return Unauthorized();
        }

        if (!Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsedRole))
        {
            return Unauthorized();
        }

        return Ok(new AuthUserDto
        {
            Id = int.Parse(userId),
            FullName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? string.Empty,
            Email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty,
            Role = parsedRole.ToString(),
            EmployeeId = int.TryParse(employeeId, out var empId) ? empId : null,
            Permissions = Authorization.HrmsRolePermissions.GetPermissions(parsedRole).ToArray()
        });
    }
}
