using CRM.DATA;
using CRM.DTO;
using CRM.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly TaskDbcontext _context;

        public AuthController(TaskDbcontext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            {
                return BadRequest("Email and password are required.");
            }

            var email = req.Email.Trim().ToLowerInvariant();
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email))
            {
                return Conflict("An account with this email already exists.");
            }

            var (roleId, roleErr) = await ResolveRegisterRoleIdAsync(req.RoleId);
            if (roleErr != null)
            {
                return roleErr;
            }

            var user = new User
            {
                FullName = req.FullName?.Trim() ?? string.Empty,
                Email = email,
                Phone = req.Phone?.Trim() ?? string.Empty,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                RoleId = roleId,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            await _context.Entry(user).Reference(u => u.Role).LoadAsync();
            return Ok(ToSession(user));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            {
                return BadRequest("Email and password are required.");
            }

            var email = req.Email.Trim().ToLowerInvariant();
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid email or password.");
            }

            return Ok(ToSession(user));
        }

        /// <summary>All users for UI lists (password hash is never loaded or returned).</summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
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
                    Role = u.Role != null ? u.Role.Name : string.Empty,
                    CreatedAt = u.CreatedAt
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

            return Ok(ToSession(user));
        }

        private async Task<(int? RoleId, IActionResult? Error)> ResolveRegisterRoleIdAsync(int? requestedRoleId)
        {
            if (requestedRoleId is int rid && rid > 0)
            {
                if (!await _context.Roles.AnyAsync(r => r.Id == rid && r.IsActive))
                {
                    return (null, BadRequest($"Role id {rid} does not exist or is inactive."));
                }

                return (rid, null);
            }

            var defaultRoleId = await _context.Roles.AsNoTracking()
                .Where(r => r.IsActive && r.Name.ToLower() == "user")
                .Select(r => (int?)r.Id)
                .FirstOrDefaultAsync();
            if (defaultRoleId == null)
            {
                return (null, BadRequest(
                    "No active role named 'user' exists. Create it via /api/MasterData/roles or send roleId."));
            }

            return (defaultRoleId, null);
        }

        private static UserSessionDto ToSession(User u)
        {
            return new UserSessionDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                RoleId = u.RoleId,
                Role = u.Role?.Name ?? string.Empty,
                Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())[..22]
            };
        }
    }
}
