using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using CRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly TaskDbcontext _context;
        private readonly IRbacService _rbac;

        public AuthController(TaskDbcontext context, IRbacService rbac)
        {
            _context = context;
            _rbac = rbac;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromQuery] int? userId, [FromBody] RegisterRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            {
                return BadRequest("Email and password are required.");
            }

            if (userId is int auditUid && auditUid > 0)
            {
                var permErr = await RbacAuthorization.RequireAnyPermissionAsync(
                    _context, _rbac, auditUid, "users.create", "settings.manage");
                if (permErr != null)
                {
                    return permErr;
                }

                AuditUserValidation.SetAuditUser(_context, auditUid);
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
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            await _context.Entry(user).Reference(u => u.Role).LoadAsync();
            return Ok(await ToSessionAsync(user));
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

            if (!user.IsActive)
            {
                return Unauthorized("Account is inactive.");
            }

            return Ok(await ToSessionAsync(user));
        }

        /// <summary>All users for UI lists (password hash is never loaded or returned).</summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] int userId)
        {
            var err = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId,
                "users.view", "settings.manage", "leads.assign", "deals.assign");
            if (err != null)
            {
                return err;
            }

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
        public async Task<IActionResult> GetUser(int id, [FromQuery] int userId)
        {
            var err = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId, "users.view", "settings.manage");
            if (err != null && id != userId)
            {
                return err;
            }

            if (id != userId)
            {
                var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
                if (auditErr != null)
                {
                    return auditErr;
                }
            }

            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(await ToSessionAsync(user));
        }

        [HttpPut("users/{id:int}")]
        public async Task<IActionResult> UpdateUser(int id, [FromQuery] int userId, [FromBody] UpdateUserRequest req)
        {
            if (req == null)
            {
                return BadRequest();
            }

            var err = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId, "users.edit", "settings.manage");
            if (err != null)
            {
                return err;
            }

            AuditUserValidation.SetAuditUser(_context, userId);

            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(req.FullName))
            {
                user.FullName = req.FullName.Trim();
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
            return Ok(await ToSessionAsync(user));
        }

        /// <summary>
        /// Deletes a CRM user after the acting admin verifies their own password.
        /// </summary>
        [HttpDelete("users/{id:int}")]
        public async Task<IActionResult> DeleteUser(int id, [FromQuery] int userId, [FromBody] DeleteUserRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Password))
            {
                return BadRequest("Password is required to delete a user.");
            }

            if (userId <= 0)
            {
                return BadRequest("A valid acting user id is required.");
            }

            var permErr = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId, "users.delete", "settings.manage");
            if (permErr != null)
            {
                return permErr;
            }

            var actingUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (actingUser == null || !actingUser.IsActive)
            {
                return Unauthorized("Your session is invalid.");
            }

            if (!BCrypt.Net.BCrypt.Verify(req.Password, actingUser.PasswordHash))
            {
                return Unauthorized("Incorrect password.");
            }

            if (id == userId)
            {
                return BadRequest("You cannot delete your own account.");
            }

            var target = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (target == null)
            {
                return NotFound();
            }

            if (target.RoleId == AdminUserValidation.AdminRoleId)
            {
                var otherActiveAdmins = await _context.Users.CountAsync(u =>
                    u.Id != id && u.IsActive && u.RoleId == AdminUserValidation.AdminRoleId);
                if (otherActiveAdmins == 0)
                {
                    return BadRequest("Cannot delete the last active admin account.");
                }
            }

            AuditUserValidation.SetAuditUser(_context, userId);
            _context.Users.Remove(target);
            await _context.SaveChangesAsync();

            return NoContent();
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
                    "No active role named 'user' exists. Create it via /api/rbac/roles or send roleId."));
            }

            return (defaultRoleId, null);
        }

        private async Task<UserSessionDto> ToSessionAsync(User u)
        {
            var perms = await _rbac.GetUserPermissionsAsync(u.Id);
            return new UserSessionDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                RoleId = u.RoleId,
                Role = u.Role?.Name ?? string.Empty,
                Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())[..22],
                Permissions = perms,
            };
        }
    }
}
