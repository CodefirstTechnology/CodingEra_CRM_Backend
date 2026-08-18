using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using CRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers
{
    [Route("api/contacts")]
    [ApiController]
    public class ContactsController : ControllerBase
    {
        private readonly TaskDbcontext _context;
        private readonly IRbacService _rbac;
        private readonly IContactImportService _contactImportService;
        private readonly IContactImportFileParser _contactImportFileParser;

        public ContactsController(
            TaskDbcontext context,
            IRbacService rbac,
            IContactImportService contactImportService,
            IContactImportFileParser contactImportFileParser)
        {
            _context = context;
            _rbac = rbac;
            _contactImportService = contactImportService;
            _contactImportFileParser = contactImportFileParser;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int userId,
            [FromQuery] int? organizationId = null,
            [FromQuery] string? search = null,
            [FromQuery] int limit = 20)
        {
            var permErr = await RbacAuthorization.RequirePermissionAsync(_context, _rbac, userId, "contacts.view");
            if (permErr != null) return permErr;

            var q = _context.Contacts.AsNoTracking();
            if (organizationId.HasValue)
            {
                q = q.Where(c => c.OrganizationId == organizationId);
            }

            var term = search?.Trim();
            if (!string.IsNullOrWhiteSpace(term) && term.Length >= 2)
            {
                var pattern = $"%{term}%";
                q = q.Where(c =>
                    EF.Functions.ILike(c.FirstName, pattern)
                    || EF.Functions.ILike(c.LastName, pattern)
                    || EF.Functions.ILike(c.Email, pattern)
                    || EF.Functions.ILike(c.Phone, pattern)
                    || EF.Functions.ILike(c.FirstName + " " + c.LastName, pattern));
            }

            q = await RbacRecordScopeHelper.ApplyCreatedByScopeAsync(_context, _rbac, userId, "contacts", q);

            q = q.OrderBy(c => c.LastName).ThenBy(c => c.FirstName);
            if (!string.IsNullOrWhiteSpace(term))
            {
                var take = Math.Clamp(limit, 1, 50);
                q = q.Take(take);
            }

            return Ok(await q.ToListAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, [FromQuery] int userId)
        {
            var permErr = await RbacAuthorization.RequirePermissionAsync(_context, _rbac, userId, "contacts.view");
            if (permErr != null) return permErr;

            var c = await _context.Contacts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (c == null)
            {
                return NotFound();
            }

            if (!await RbacRecordScopeHelper.CanAccessCreatedByRecordAsync(_context, _rbac, userId, "contacts", c.CreatedBy))
            {
                return NotFound();
            }

            return Ok(c);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] int userId, [FromBody] ContactUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var permErr = await RbacAuthorization.RequirePermissionAsync(_context, _rbac, userId, "contacts.create");
            if (permErr != null) return permErr;

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null)
            {
                return auditErr;
            }

            AuditUserValidation.SetAuditUser(_context, userId);

            var entity = CrmWriteMappings.ToContact(dto, 0);
            entity.Id = 0;
            await _context.Contacts.AddAsync(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromQuery] int userId, [FromBody] ContactUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var permErr = await RbacAuthorization.RequirePermissionAsync(_context, _rbac, userId, "contacts.edit");
            if (permErr != null) return permErr;

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null)
            {
                return auditErr;
            }

            AuditUserValidation.SetAuditUser(_context, userId);

            if (dto.Id != 0 && dto.Id != id)
            {
                return BadRequest("Route id and body id must match when the body includes an id.");
            }

            var existing = await _context.Contacts.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            if (!await RbacRecordScopeHelper.CanAccessCreatedByRecordAsync(_context, _rbac, userId, "contacts", existing.CreatedBy))
            {
                return NotFound();
            }

            CrmWriteMappings.Apply(existing, dto);
            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] int userId)
        {
            var permErr = await RbacAuthorization.RequirePermissionAsync(_context, _rbac, userId, "contacts.delete");
            if (permErr != null) return permErr;

            var entity = await _context.Contacts.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            if (!await RbacRecordScopeHelper.CanAccessCreatedByRecordAsync(_context, _rbac, userId, "contacts", entity.CreatedBy))
            {
                return NotFound();
            }

            _context.Contacts.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { deleted = true });
        }

        /// <summary>Validates import rows against CRM master data and duplicate rules. Does not persist contacts.</summary>
        [HttpPost("import")]
        [Consumes("application/json", "multipart/form-data")]
        [RequestSizeLimit(104_857_600)]
        public async Task<IActionResult> ValidateImport([FromQuery] int userId)
        {
            var permErr = await RbacAuthorization.RequirePermissionAsync(_context, _rbac, userId, "contacts.import");
            if (permErr != null) return permErr;
            var resolved = await ResolveImportRowsAsync();
            if (resolved.Error != null)
            {
                return resolved.Error;
            }

            var result = await _contactImportService.ValidateImportAsync(resolved.Rows!);
            return Ok(result);
        }

        /// <summary>Validates and persists valid import rows. Skips duplicate and invalid rows.</summary>
        [HttpPost("import/commit")]
        [Consumes("application/json", "multipart/form-data")]
        [RequestSizeLimit(104_857_600)]
        public async Task<IActionResult> CommitImport([FromQuery] int userId)
        {
            var permErr = await RbacAuthorization.RequirePermissionAsync(_context, _rbac, userId, "contacts.import");
            if (permErr != null) return permErr;

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null)
            {
                return auditErr;
            }

            var resolved = await ResolveImportRowsAsync();
            if (resolved.Error != null)
            {
                return resolved.Error;
            }

            var result = await _contactImportService.CommitImportAsync(userId, resolved.Rows!);
            return Ok(result);
        }

        private async Task<(IReadOnlyList<ContactImportRowDto>? Rows, IActionResult? Error)> ResolveImportRowsAsync()
        {
            if (Request.HasFormContentType)
            {
                var file = Request.Form.Files.GetFile("file") ?? Request.Form.Files.FirstOrDefault();
                if (file == null || file.Length == 0)
                {
                    return (null, BadRequest("Import file is required."));
                }

                var fileName = file.FileName ?? string.Empty;
                if (!fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
                    && !fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    return (null, BadRequest("Only .xlsx and .csv files are supported."));
                }

                try
                {
                    await using var stream = file.OpenReadStream();
                    var rows = await _contactImportFileParser.ParseAsync(stream, fileName, HttpContext.RequestAborted);
                    if (rows.Count == 0)
                    {
                        return (null, BadRequest("At least one import row is required."));
                    }

                    return (rows, null);
                }
                catch (InvalidOperationException ex)
                {
                    return (null, BadRequest(ex.Message));
                }
            }

            var dto = await Request.ReadFromJsonAsync<ContactImportRequestDto>(HttpContext.RequestAborted);
            if (dto?.Rows == null || dto.Rows.Count == 0)
            {
                return (null, BadRequest("At least one import row is required."));
            }

            return (dto.Rows, null);
        }
    }
}
