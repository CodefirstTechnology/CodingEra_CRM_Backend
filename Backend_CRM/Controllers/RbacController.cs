using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using CRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers
{
    /// <summary>Dynamic role &amp; permission management (ERPNext/Frappe style RBAC).</summary>
    [Route("api/rbac")]
    [ApiController]
    public class RbacController : ControllerBase
    {
        private readonly TaskDbcontext _context;
        private readonly IRbacService _rbac;
        private readonly ILogger<RbacController> _logger;

        public RbacController(TaskDbcontext context, IRbacService rbac, ILogger<RbacController> logger)
        {
            _context = context;
            _rbac = rbac;
            _logger = logger;
        }

        [HttpGet("permissions")]
        public async Task<IActionResult> GetPermissions([FromQuery] int userId)
        {
            var err = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId, _logger, "roles.view", "roles.manage", "settings.manage");
            if (err != null)
            {
                return err;
            }

            var perms = await _context.Permissions.AsNoTracking()
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Action)
                .Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Module = p.Module,
                    Action = p.Action,
                    Code = p.Code,
                    Description = p.Description,
                })
                .ToListAsync();

            var grouped = perms
                .GroupBy(p => p.Module)
                .Select(g => new { module = g.Key, permissions = g.ToList() })
                .ToList();

            return Ok(grouped);
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles(
            [FromQuery] int userId,
            [FromQuery] string? search = null,
            [FromQuery] bool activeOnly = false)
        {
            var err = await RbacAuthorization.RequireAnyPermissionAsync(
                _context,
                _rbac,
                userId,
                _logger,
                "roles.view",
                "roles.manage",
                "settings.manage",
                "users.view",
                "users.create");
            if (err != null)
            {
                return err;
            }

            IQueryable<Role> q = _context.Roles.AsNoTracking();
            if (activeOnly)
            {
                q = q.Where(r => r.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                q = q.Where(r => r.Name.ToLower().Contains(term) || r.Description.ToLower().Contains(term));
            }

            var roles = await q
                .OrderBy(r => r.Name)
                .Select(r => new RoleListItemDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    CreatedAt = r.CreatedAt,
                    AssignedUserCount = _context.Users.Count(u => u.RoleId == r.Id),
                })
                .ToListAsync();

            return Ok(roles);
        }

        [HttpGet("roles/{id:int}")]
        public async Task<IActionResult> GetRole(int id, [FromQuery] int userId)
        {
            var err = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId, _logger, "roles.view", "roles.manage", "settings.manage");
            if (err != null)
            {
                return err;
            }

            var role = await _context.Roles.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);
            if (role == null)
            {
                return NotFound();
            }

            var assignments = await _context.RolePermissions.AsNoTracking()
                .Where(rp => rp.RoleId == id)
                .Join(
                    _context.Permissions.AsNoTracking(),
                    rp => rp.PermissionId,
                    p => p.Id,
                    (rp, p) => new RolePermissionAssignmentDto
                    {
                        PermissionId = p.Id,
                        Code = p.Code,
                        AccessScope = rp.AccessScope,
                    })
                .ToListAsync();

            var dto = new RoleDetailDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsActive = role.IsActive,
                AssignedUserCount = await _context.Users.CountAsync(u => u.RoleId == id),
                Permissions = assignments,
            };

            return Ok(dto);
        }

        [HttpPost("roles")]
        public async Task<IActionResult> CreateRole([FromQuery] int userId, [FromBody] RoleUpsertDto dto)
        {
            var err = await RbacAuthorization.RequirePermissionAsync(_context, _rbac, userId, "roles.manage", _logger);
            if (err != null)
            {
                return err;
            }

            AuditUserValidation.SetAuditUser(_context, userId);

            var name = (dto.Name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest("Role name is required.");
            }

            if (await _context.Roles.AnyAsync(r => r.Name.ToLower() == name.ToLower()))
            {
                return Conflict("A role with this name already exists.");
            }

            var entity = new Role
            {
                Name = name,
                Description = dto.Description?.Trim() ?? string.Empty,
                IsActive = dto.IsActive,
            };

            await _context.Roles.AddAsync(entity);
            await _context.SaveChangesAsync();
            return Ok(new RoleListItemDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                AssignedUserCount = 0,
            });
        }

        [HttpPut("roles/{id:int}")]
        public async Task<IActionResult> UpdateRole(int id, [FromQuery] int userId, [FromBody] RoleUpsertDto dto)
        {
            var err = await RbacAuthorization.RequirePermissionAsync(_context, _rbac, userId, "roles.manage", _logger);
            if (err != null)
            {
                return err;
            }

            AuditUserValidation.SetAuditUser(_context, userId);

            var existing = await _context.Roles.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            var name = (dto.Name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest("Role name is required.");
            }

            if (await _context.Roles.AnyAsync(r => r.Name.ToLower() == name.ToLower() && r.Id != id))
            {
                return Conflict("A role with this name already exists.");
            }

            existing.Name = name;
            existing.Description = dto.Description?.Trim() ?? string.Empty;
            existing.IsActive = dto.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new RoleListItemDto
            {
                Id = existing.Id,
                Name = existing.Name,
                Description = existing.Description,
                IsActive = existing.IsActive,
                CreatedAt = existing.CreatedAt,
                AssignedUserCount = await _context.Users.CountAsync(u => u.RoleId == id),
            });
        }

        [HttpPut("roles/{id:int}/permissions")]
        public async Task<IActionResult> UpdateRolePermissions(
            int id,
            [FromQuery] int userId,
            [FromBody] RolePermissionsUpdateDto? dto)
        {
            var err = await RbacAuthorization.RequirePermissionAsync(_context, _rbac, userId, "roles.manage", _logger);
            if (err != null)
            {
                return err;
            }

            if (dto == null)
            {
                return BadRequest("Request body is required.");
            }

            AuditUserValidation.SetAuditUser(_context, userId);

            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            var existing = await _context.RolePermissions.Where(rp => rp.RoleId == id).ToListAsync();
            _context.RolePermissions.RemoveRange(existing);

            foreach (var item in dto.Permissions ?? Array.Empty<RolePermissionAssignmentDto>())
            {
                if (item.PermissionId <= 0)
                {
                    continue;
                }

                if (!await _context.Permissions.AnyAsync(p => p.Id == item.PermissionId))
                {
                    continue;
                }

                await _context.RolePermissions.AddAsync(new RolePermission
                {
                    RoleId = id,
                    PermissionId = item.PermissionId,
                    AccessScope = item.AccessScope,
                });
            }

            await _context.SaveChangesAsync();
            return await GetRole(id, userId);
        }

        [HttpPost("roles/{id:int}/clone")]
        public async Task<IActionResult> CloneRole(int id, [FromQuery] int userId, [FromBody] RoleUpsertDto dto)
        {
            var err = await RbacAuthorization.RequirePermissionAsync(_context, _rbac, userId, "roles.manage", _logger);
            if (err != null)
            {
                return err;
            }

            AuditUserValidation.SetAuditUser(_context, userId);

            var source = await _context.Roles.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);
            if (source == null)
            {
                return NotFound();
            }

            var name = (dto.Name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest("New role name is required.");
            }

            if (await _context.Roles.AnyAsync(r => r.Name.ToLower() == name.ToLower()))
            {
                return Conflict("A role with this name already exists.");
            }

            var clone = new Role
            {
                Name = name,
                Description = dto.Description?.Trim() ?? source.Description,
                IsActive = dto.IsActive,
            };
            await _context.Roles.AddAsync(clone);
            await _context.SaveChangesAsync();

            var sourcePerms = await _context.RolePermissions.AsNoTracking()
                .Where(rp => rp.RoleId == id)
                .ToListAsync();

            foreach (var sp in sourcePerms)
            {
                await _context.RolePermissions.AddAsync(new RolePermission
                {
                    RoleId = clone.Id,
                    PermissionId = sp.PermissionId,
                    AccessScope = sp.AccessScope,
                });
            }

            await _context.SaveChangesAsync();
            return await GetRole(clone.Id, userId);
        }

        [HttpDelete("roles/{id:int}")]
        public async Task<IActionResult> DeleteRole(int id, [FromQuery] int userId)
        {
            var err = await RbacAuthorization.RequirePermissionAsync(_context, _rbac, userId, "roles.manage", _logger);
            if (err != null)
            {
                return err;
            }

            var assigned = await _context.Users.CountAsync(u => u.RoleId == id);
            if (assigned > 0)
            {
                return BadRequest($"Cannot delete role: {assigned} user(s) are assigned to this role.");
            }

            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            var perms = await _context.RolePermissions.Where(rp => rp.RoleId == id).ToListAsync();
            _context.RolePermissions.RemoveRange(perms);
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
            return Ok(new { deleted = true });
        }

        [HttpGet("me/permissions")]
        public async Task<IActionResult> GetMyPermissions([FromQuery] int userId)
        {
            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null)
            {
                return auditErr;
            }

            var perms = await _rbac.GetUserPermissionsAsync(userId);
            return Ok(perms);
        }

        /// <summary>Debug aid — shows role and effective permissions for the acting user.</summary>
        [HttpGet("me/access")]
        public async Task<IActionResult> GetMyAccess([FromQuery] int userId)
        {
            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null)
            {
                return auditErr;
            }

            return Ok(await _rbac.GetAccessDiagnosticAsync(userId));
        }
    }
}
