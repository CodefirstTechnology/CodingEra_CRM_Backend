using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ERP.API.Security;
using ERP.Domain.Entities;
using ERP.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ERP.API.Controllers
{
    // --- DTO Contracts ---

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Password { get; set; } = string.Empty;
        public int? RoleId { get; set; }
    }

    public class UpdateUserRequest
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public int? RoleId { get; set; }
        public bool? IsActive { get; set; }
    }

    public class ChangePasswordRequest
    {
        public int? UserId { get; set; }
        public string CurrentPassword { get; set; } = string.Empty;
        public string OldPassword { get; set; } = string.Empty; // Alias for contract flexibility
        public string NewPassword { get; set; } = string.Empty;
    }

    public class LogoutRequest
    {
        public int? UserId { get; set; }
        public string? Email { get; set; }
    }

    public class UserSessionDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public int? RoleId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public DateTime? LastActiveAt { get; set; }
        public DateTime? FirstLoginAt { get; set; }
    }

    public class UserListItemDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public int? RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsOnline { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastActiveAt { get; set; }
    }

    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateRoleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    // --- Controller ---

    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ERPDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ERPDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            {
                return BadRequest("Email and password are required.");
            }

            var cleanEmail = req.Email.Trim().ToLowerInvariant();
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == cleanEmail))
            {
                return Conflict("An account with this email already exists.");
            }

            var roleId = req.RoleId;
            if (roleId is int rId && rId > 0)
            {
                if (!await _context.Roles.AnyAsync(r => r.Id == rId && r.IsActive))
                {
                    return BadRequest($"Role id {rId} does not exist or is inactive.");
                }
            }
            else
            {
                roleId = await _context.Roles
                    .Where(r => r.IsActive)
                    .Select(r => (int?)r.Id)
                    .FirstOrDefaultAsync() ?? 1;
            }

            var now = DateTime.UtcNow;
            var user = new User
            {
                FullName = req.FullName?.Trim() ?? string.Empty,
                Email = cleanEmail,
                Phone = req.Phone?.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                RoleId = roleId,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            await _context.Entry(user).Reference(u => u.Role).LoadAsync();
            return Ok(ToSessionDto(user));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            {
                return BadRequest("Email and password are required.");
            }

            var cleanEmail = req.Email.Trim().ToLowerInvariant();
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == cleanEmail);

            if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid email or password.");
            }

            if (!user.IsActive)
            {
                return Unauthorized("Account is inactive.");
            }

            var nowUtc = DateTime.UtcNow;
            user.IsOnline = true;
            user.LastActiveAt = nowUtc;
            if (user.FirstLoginAt == null)
            {
                user.FirstLoginAt = nowUtc;
            }
            await _context.SaveChangesAsync();

            return Ok(ToSessionDto(user));
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromQuery] int? userId, [FromQuery] string? email, [FromBody] LogoutRequest? body)
        {
            int? targetId = userId ?? body?.UserId;
            string? targetEmail = !string.IsNullOrWhiteSpace(email) ? email : body?.Email;

            User? user = null;
            if (targetId is int uid && uid > 0)
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Id == uid);
            }
            if (user == null && !string.IsNullOrWhiteSpace(targetEmail))
            {
                var clean = targetEmail.Trim().ToLowerInvariant();
                user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == clean);
            }

            if (user != null)
            {
                user.IsOnline = false;
                user.LastActiveAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Logged out successfully." });
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var cutoffOnline = DateTime.UtcNow.AddMinutes(-10);
            var users = await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .OrderBy(u => u.FullName)
                .ThenBy(u => u.Email)
                .Select(u => new UserListItemDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.Phone,
                    RoleId = u.RoleId,
                    RoleName = u.Role != null ? u.Role.Name : (u.RoleId == 1 ? "Admin" : "User"),
                    IsActive = u.IsActive,
                    IsOnline = u.IsOnline && u.LastActiveAt != null && u.LastActiveAt.Value >= cutoffOnline,
                    CreatedAt = u.CreatedAt,
                    LastActiveAt = u.LastActiveAt,
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("users/{id:int}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(ToSessionDto(user));
        }

        [HttpPut("users/{id:int}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest req)
        {
            if (req == null)
            {
                return BadRequest("Invalid payload.");
            }

            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(req.FullName))
            {
                user.FullName = req.FullName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(req.Email))
            {
                var cleanEmail = req.Email.Trim().ToLowerInvariant();
                if (cleanEmail != user.Email && await _context.Users.AnyAsync(u => u.Id != id && u.Email.ToLower() == cleanEmail))
                {
                    return Conflict("Email is already in use by another account.");
                }
                user.Email = cleanEmail;
            }

            if (req.Phone != null)
            {
                user.Phone = req.Phone.Trim();
            }

            if (req.IsActive.HasValue)
            {
                user.IsActive = req.IsActive.Value;
            }

            if (req.RoleId is int newRoleId && newRoleId > 0)
            {
                if (!await _context.Roles.AnyAsync(r => r.Id == newRoleId && r.IsActive))
                {
                    return BadRequest($"Role id {newRoleId} does not exist or is inactive.");
                }
                user.RoleId = newRoleId;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _context.Entry(user).Reference(u => u.Role).LoadAsync();

            return Ok(ToSessionDto(user));
        }

        [HttpDelete("users/{id:int}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var target = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (target == null)
            {
                return NotFound();
            }

            _context.Users.Remove(target);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromQuery] int? userId, [FromBody] ChangePasswordRequest req)
        {
            if (req == null)
            {
                return BadRequest("Invalid request.");
            }

            int targetUid = req.UserId ?? userId ?? 0;
            string currentPwd = !string.IsNullOrWhiteSpace(req.CurrentPassword) ? req.CurrentPassword : req.OldPassword;

            if (targetUid <= 0 || string.IsNullOrWhiteSpace(currentPwd) || string.IsNullOrWhiteSpace(req.NewPassword))
            {
                return BadRequest("User ID, current password, and new password are required.");
            }

            if (req.NewPassword.Length < 6 || req.NewPassword.Length > 200)
            {
                return BadRequest("New password must be between 6 and 200 characters.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == targetUid);
            if (user == null || !user.IsActive)
            {
                return Unauthorized("User session or account is invalid.");
            }

            if (!BCrypt.Net.BCrypt.Verify(currentPwd, user.PasswordHash))
            {
                return Unauthorized("Incorrect current password.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password updated successfully." });
        }

        // --- Role Management Endpoints ---

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _context.Roles
                .AsNoTracking()
                .OrderBy(r => r.Id)
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    IsActive = r.IsActive
                })
                .ToListAsync();

            return Ok(roles);
        }

        [HttpPost("roles")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Name))
            {
                return BadRequest("Role name is required.");
            }

            var cleanName = req.Name.Trim();
            if (await _context.Roles.AnyAsync(r => r.Name.ToLower() == cleanName.ToLower()))
            {
                return Conflict($"Role '{cleanName}' already exists.");
            }

            var role = new Role
            {
                Name = cleanName,
                Description = req.Description?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Roles.AddAsync(role);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRoles), new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsActive = role.IsActive
            });
        }

        // --- Helper Methods ---

        private UserSessionDto ToSessionDto(User user)
        {
            var roleName = user.Role?.Name ?? (user.RoleId == 1 ? "Admin" : "User");
            var token = GenerateJwtToken(user, roleName);

            return new UserSessionDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                RoleId = user.RoleId,
                Role = roleName,
                Token = token,
                IsOnline = user.IsOnline,
                LastActiveAt = user.LastActiveAt,
                FirstLoginAt = user.FirstLoginAt
            };
        }

        private string GenerateJwtToken(User user, string roleName)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var keyString = jwtSection["Key"] ?? JwtAuthenticationExtensions.DefaultJwtKey;
            var issuer = jwtSection["Issuer"] ?? JwtAuthenticationExtensions.DefaultIssuer;
            var audience = jwtSection["Audience"] ?? JwtAuthenticationExtensions.DefaultAudience;

            var keyBytes = Encoding.UTF8.GetBytes(keyString);
            if (keyBytes.Length < 32)
            {
                Array.Resize(ref keyBytes, 32);
            }

            var claims = new[]
            {
                new Claim("userId", user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("fullName", user.FullName),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim("role", roleName),
                new Claim(ClaimTypes.Role, roleName)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(keyBytes),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenObj = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(tokenObj);
        }
    }
}
