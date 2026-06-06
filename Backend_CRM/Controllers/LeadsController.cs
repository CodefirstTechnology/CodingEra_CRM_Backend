using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using CRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers
{
    [Route("api/leads")]
    [ApiController]
    public class LeadsController : ControllerBase
    {
        private readonly TaskDbcontext _context;
        private readonly ILeadImportService _leadImportService;
        private readonly ILeadImportFileParser _leadImportFileParser;
        private readonly IRbacService _rbac;

        public LeadsController(
            TaskDbcontext context,
            ILeadImportService leadImportService,
            ILeadImportFileParser leadImportFileParser,
            IRbacService rbac)
        {
            _context = context;
            _leadImportService = leadImportService;
            _leadImportFileParser = leadImportFileParser;
            _rbac = rbac;
        }

        private static IQueryable<Lead> QueryWithMasters(IQueryable<Lead> q) =>
            q.Include(l => l.Salutation)
                .Include(l => l.LeadStatus)
                .Include(l => l.RequestType)
                .Include(l => l.LeadOwner)
                .Include(l => l.Organization)
                .ThenInclude(o => o!.Industry)
                .Include(l => l.Organization)
                .ThenInclude(o => o!.EmployeeCount)
                .Include(l => l.Organization)
                .ThenInclude(o => o!.Territory);

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int userId, [FromQuery] string? leadSource = null, [FromQuery] string? status = null)
        {
            var permErr = await RbacAuthorization.RequirePermissionAsync(_context, _rbac, userId, "leads.view");
            if (permErr != null) return permErr;
            IQueryable<Lead> q = QueryWithMasters(_context.Leads.AsNoTracking());
            if (!string.IsNullOrWhiteSpace(leadSource))
            {
                q = q.Where(l => l.LeadSource == leadSource);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (int.TryParse(status, out var statusId))
                {
                    q = q.Where(l => l.LeadStatusId == statusId);
                }
                else
                {
                    var st = status.Trim();
                    q = q.Where(l =>
                        _context.LeadStatuses.Any(ls =>
                            ls.Id == l.LeadStatusId && ls.Name.ToLower() == st.ToLower()));
                }
            }

            return Ok(await q.OrderByDescending(l => l.UpdatedAt).AsSplitQuery().ToListAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, [FromQuery] int userId)
        {
            var permErr = await RbacAuthorization.RequirePermissionAsync(_context, _rbac, userId, "leads.view");
            if (permErr != null) return permErr;
            var l = await QueryWithMasters(_context.Leads.AsNoTracking())
                .FirstOrDefaultAsync(x => x.Id == id);
            if (l == null)
            {
                return NotFound();
            }

            return Ok(l);
        }

        /// <summary>Prior versions of this lead (newest first). Each row is the full scalar snapshot before an update.</summary>
        [HttpGet("{id:int}/history")]
        public async Task<IActionResult> GetHistory(int id, [FromQuery] int userId)
        {
            var permErr = await RbacAuthorization.RequirePermissionAsync(_context, _rbac, userId, "leads.view");
            if (permErr != null) return permErr;
            if (!await _context.Leads.AsNoTracking().AnyAsync(l => l.Id == id))
            {
                return NotFound();
            }

            var rows = await _context.LeadHistories.AsNoTracking()
                .Where(h => h.LeadId == id)
                .OrderByDescending(h => h.ArchivedAt)
                .ThenByDescending(h => h.Id)
                .ToListAsync();
            return Ok(rows);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] int userId, [FromBody] LeadUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var permErr = await RbacAuthorization.RequirePermissionAsync(_context, _rbac, userId, "leads.create");
            if (permErr != null) return permErr;

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null)
            {
                return auditErr;
            }

            AuditUserValidation.SetAuditUser(_context, userId);

            var entity = new Lead();
            ApplyDtoToLeadScalars(dto, entity);
            var masterErr = await ApplyLeadMastersFromDtoAsync(dto, entity, isCreate: true);
            if (masterErr != null)
            {
                return masterErr;
            }

            var orgError = await ApplyOrganizationFromDtoAsync(dto, entity);
            if (orgError != null)
            {
                return orgError;
            }

            await RecordOwnershipEnforcement.EnforceLeadOwnerOnCreateAsync(_rbac, userId, entity);

            entity.Id = 0;

            var now = DateTime.UtcNow;
            if (entity.CreatedAt == null && string.Equals(entity.LeadSource, "IndiaMART", StringComparison.OrdinalIgnoreCase))
            {
                entity.CreatedAt = now;
            }

            await _context.Leads.AddAsync(entity);
            await _context.SaveChangesAsync();
            return Ok(await ReloadLeadAsync(entity.Id));
        }

        /// <summary>Validates import rows against CRM master data and duplicate rules. Does not persist leads.</summary>
        [HttpPost("import")]
        [Consumes("application/json", "multipart/form-data")]
        [RequestSizeLimit(104_857_600)]
        public async Task<IActionResult> ValidateImport([FromQuery] int userId)
        {
            var permErr = await RbacAuthorization.RequirePermissionAsync(_context, _rbac, userId, "leads.import");
            if (permErr != null) return permErr;
            var resolved = await ResolveImportRowsAsync();
            if (resolved.Error != null)
            {
                return resolved.Error;
            }

            var result = await _leadImportService.ValidateImportAsync(resolved.Rows!);
            return Ok(result);
        }

        /// <summary>Validates and persists valid import rows. Skips duplicate and invalid rows.</summary>
        [HttpPost("import/commit")]
        [Consumes("application/json", "multipart/form-data")]
        [RequestSizeLimit(104_857_600)]
        public async Task<IActionResult> CommitImport([FromQuery] int userId)
        {
            var permErr = await RbacAuthorization.RequirePermissionAsync(_context, _rbac, userId, "leads.import");
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

            var result = await _leadImportService.CommitImportAsync(userId, resolved.Rows!);
            return Ok(result);
        }

        private async Task<(IReadOnlyList<LeadImportRowDto>? Rows, IActionResult? Error)> ResolveImportRowsAsync()
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
                    var rows = await _leadImportFileParser.ParseAsync(stream, fileName, HttpContext.RequestAborted);
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

            var dto = await Request.ReadFromJsonAsync<LeadImportRequestDto>(HttpContext.RequestAborted);
            if (dto?.Rows == null || dto.Rows.Count == 0)
            {
                return (null, BadRequest("At least one import row is required."));
            }

            return (dto.Rows, null);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromQuery] int userId, [FromBody] LeadUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var permErr = await RbacAuthorization.RequirePermissionAsync(_context, _rbac, userId, "leads.edit");
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

            var existing = await _context.Leads
                .Include(l => l.Organization)
                .FirstOrDefaultAsync(l => l.Id == id);
            if (existing == null)
            {
                return NotFound();
            }

            await RecordOwnershipEnforcement.EnforceLeadOwnerOnUpdateAsync(_rbac, userId, dto, existing);

            ApplyDtoToLeadScalars(dto, existing);
            var masterErr = await ApplyLeadMastersFromDtoAsync(dto, existing, isCreate: false);
            if (masterErr != null)
            {
                return masterErr;
            }

            var orgError = await ApplyOrganizationFromDtoAsync(dto, existing);
            if (orgError != null)
            {
                return orgError;
            }

            existing.CreatedAt = dto.CreatedAt;

            await _context.SaveChangesAsync();
            return Ok(await ReloadLeadAsync(existing.Id));
        }

        private async Task<Lead> ReloadLeadAsync(int id) =>
            await QueryWithMasters(_context.Leads.AsNoTracking()).FirstAsync(l => l.Id == id);

        private static void ApplyDtoToLeadScalars(LeadUpsertDto from, Lead to)
        {
            to.FirstName = from.FirstName;
            to.LastName = from.LastName;
            to.Gender = from.Gender;
            to.Mobile = from.Mobile;
            to.Email = from.Email;
            to.Notes = from.Notes;
            to.LeadOwnerId = from.LeadOwnerId;
            to.LeadSource = from.LeadSource;
            to.CreatedAt = from.CreatedAt;
        }

        private async Task<IActionResult?> ApplyLeadMastersFromDtoAsync(LeadUpsertDto dto, Lead lead, bool isCreate)
        {
            // Do not null navigations — EF can clear FK columns when the navigation is set to null.

            if (dto.SalutationId is int sid && sid > 0)
            {
                if (!await _context.Salutations.AnyAsync(s => s.Id == sid && s.IsActive))
                {
                    return BadRequest($"Salutation id {sid} does not exist or is inactive.");
                }

                lead.SalutationId = sid;
            }
            else if (isCreate)
            {
                lead.SalutationId = null;
            }

            if (dto.LeadStatusId is int lstid && lstid > 0)
            {
                if (!await _context.LeadStatuses.AnyAsync(s => s.Id == lstid && s.IsActive))
                {
                    return BadRequest($"Lead status id {lstid} does not exist or is inactive.");
                }

                lead.LeadStatusId = lstid;
            }
            else if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                lead.LeadStatusId = await ResolveNameToIdAsync(_context.LeadStatuses, dto.Status, requireActive: true);
            }
            else if (isCreate)
            {
                lead.LeadStatusId = await ResolveNameToIdAsync(_context.LeadStatuses, "New", requireActive: true);
            }

            if (dto.RequestTypeId is int rtid && rtid > 0)
            {
                if (!await _context.RequestTypes.AnyAsync(r => r.Id == rtid && r.IsActive))
                {
                    return BadRequest($"Request type id {rtid} does not exist or is inactive.");
                }

                lead.RequestTypeId = rtid;
            }
            else if (isCreate)
            {
                lead.RequestTypeId = null;
            }

            return null;
        }

        private async Task<IActionResult?> ApplyOrganizationFromDtoAsync(LeadUpsertDto dto, Lead lead)
        {
            // Do NOT set lead.Organization = null — EF clears OrganizationId when the navigation is nulled.
            var existingOrgId = lead.OrganizationId;

            if (dto.OrganizationId is int oid && oid > 0)
            {
                var exists = await _context.Organizations.AnyAsync(o => o.Id == oid);
                if (!exists)
                {
                    return BadRequest($"Organization id {oid} does not exist.");
                }

                lead.OrganizationId = oid;
                return null;
            }

            if (!dto.OrganizationId.HasValue)
            {
                if (!string.IsNullOrWhiteSpace(dto.OrganizationName))
                {
                    var orgId = await ResolveOrganizationIdByNameAsync(dto.OrganizationName);
                    if (orgId is > 0)
                    {
                        lead.OrganizationId = orgId;
                        return null;
                    }
                }

                // Partial PUT omitted organizationId — restore FK (it may have been loaded via Include).
                lead.OrganizationId = existingOrgId;
                return null;
            }

            // Explicit clear (organizationId: 0 in JSON).
            lead.OrganizationId = null;
            return null;
        }

        private async Task<int?> ResolveOrganizationIdByNameAsync(string name)
        {
            var trimmed = name.Trim();
            if (trimmed.Length == 0)
            {
                return null;
            }

            var tl = trimmed.ToLowerInvariant();
            return await _context.Organizations
                .AsNoTracking()
                .Where(o => o.Name.ToLower() == tl)
                .OrderBy(o => o.Id)
                .Select(o => (int?)o.Id)
                .FirstOrDefaultAsync();
        }

        private static async Task<int?> ResolveNameToIdAsync<TEntity>(
            DbSet<TEntity> set,
            string? name,
            bool requireActive)
            where TEntity : class
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var trimmed = name.Trim();
            var tl = trimmed.ToLowerInvariant();
            var q = set.AsNoTracking().Where(e => EF.Property<string>(e, "Name").ToLower() == tl);
            if (requireActive)
            {
                q = q.Where(e => EF.Property<bool>(e, "IsActive"));
            }

            return await q.Select(e => (int?)EF.Property<int>(e, "Id")).FirstOrDefaultAsync();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] int userId)
        {
            var permErr = await RbacAuthorization.RequirePermissionAsync(_context, _rbac, userId, "leads.delete");
            if (permErr != null) return permErr;

            var entity = await _context.Leads.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            _context.Leads.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { deleted = true });
        }
    }
}
