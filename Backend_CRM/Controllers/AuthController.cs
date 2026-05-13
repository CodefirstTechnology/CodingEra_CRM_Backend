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

            var user = new User
            {
                FullName = req.FullName?.Trim() ?? string.Empty,
                Email = email,
                Phone = req.Phone?.Trim() ?? string.Empty,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                Role = string.IsNullOrWhiteSpace(req.Role) ? "user" : req.Role.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

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
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
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
                .OrderBy(u => u.FullName)
                .ThenBy(u => u.Email)
                .Select(u => new UserListItemDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.Phone,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("users/{id:int}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(ToSession(user));
        }

        private static UserSessionDto ToSession(User u)
        {
            return new UserSessionDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                Role = u.Role,
                Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())[..22]
            };
        }
    }
}
