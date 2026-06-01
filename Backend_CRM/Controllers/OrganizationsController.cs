using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers
{
    [Route("api/organizations")]
    [ApiController]
    public class OrganizationsController : ControllerBase
    {
        private readonly TaskDbcontext _context;

        public OrganizationsController(TaskDbcontext context)
        {
            _context = context;
        }

        private static IQueryable<Organization> QueryWithMasters(IQueryable<Organization> q) =>
            q.Include(o => o.Industry).Include(o => o.EmployeeCount).Include(o => o.Territory);

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int userId)
        {
            _ = userId;
            return Ok(await QueryWithMasters(_context.Organizations.AsNoTracking()).OrderBy(o => o.Name).ToListAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, [FromQuery] int userId)
        {
            _ = userId;
            var o = await QueryWithMasters(_context.Organizations.AsNoTracking()).FirstOrDefaultAsync(x => x.Id == id);
            if (o == null)
            {
                return NotFound();
            }

            return Ok(o);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] int userId, [FromBody] OrganizationUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null)
            {
                return auditErr;
            }

            AuditUserValidation.SetAuditUser(_context, userId);

            var entity = MapDtoToNewOrganization(dto);
            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                return BadRequest("Name is required.");
            }

            var err = await ValidateOrganizationMasterIdsAsync(
                entity.IndustryId,
                entity.EmployeeCountId,
                entity.TerritoryId);
            if (err != null)
            {
                return err;
            }

            await _context.Organizations.AddAsync(entity);
            await _context.SaveChangesAsync();
            return Ok(await QueryWithMasters(_context.Organizations.AsNoTracking()).FirstAsync(o => o.Id == entity.Id));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromQuery] int userId, [FromBody] OrganizationUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

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

            var existing = await _context.Organizations.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            ApplyDtoToOrganizationForUpdate(dto, existing);
            if (string.IsNullOrWhiteSpace(existing.Name))
            {
                return BadRequest("Name is required.");
            }

            var err = await ValidateOrganizationMasterIdsAsync(
                existing.IndustryId,
                existing.EmployeeCountId,
                existing.TerritoryId);
            if (err != null)
            {
                return err;
            }

            await _context.SaveChangesAsync();
            return Ok(await QueryWithMasters(_context.Organizations.AsNoTracking()).FirstAsync(o => o.Id == id));
        }

        private static Organization MapDtoToNewOrganization(OrganizationUpsertDto dto)
        {
            var o = new Organization { Id = 0 };
            ApplyDtoToOrganization(dto, o);
            return o;
        }

        private static void ApplyDtoToOrganization(OrganizationUpsertDto dto, Organization o)
        {
            o.Name = (dto.Name ?? string.Empty).Trim();
            o.Website = (dto.Website ?? string.Empty).Trim();
            o.Gst = (dto.Gst ?? string.Empty).Trim();
            o.AnnualRevenue = dto.AnnualRevenue;
            o.IndustryId = NormalizeFk(dto.IndustryId);
            o.EmployeeCountId = NormalizeFk(dto.EmployeeCountId);
            o.TerritoryId = NormalizeFk(dto.TerritoryId);
        }

        /// <summary>
        /// PATCH-style PUT: only overwrites FKs and revenue when the client sent a value (nullable HasValue),
        /// so partial bodies from lead save do not clear territory/industry/employee-count.
        /// </summary>
        private static void ApplyDtoToOrganizationForUpdate(OrganizationUpsertDto dto, Organization o)
        {
            var name = (dto.Name ?? string.Empty).Trim();
            if (name.Length > 0)
            {
                o.Name = name;
            }

            if (dto.Website != null)
            {
                o.Website = dto.Website.Trim();
            }

            if (dto.Gst != null)
            {
                o.Gst = dto.Gst.Trim();
            }

            if (dto.AnnualRevenue.HasValue)
            {
                o.AnnualRevenue = dto.AnnualRevenue;
            }

            if (dto.IndustryId.HasValue)
            {
                o.IndustryId = NormalizeFk(dto.IndustryId);
            }

            if (dto.EmployeeCountId.HasValue)
            {
                o.EmployeeCountId = NormalizeFk(dto.EmployeeCountId);
            }

            if (dto.TerritoryId.HasValue)
            {
                o.TerritoryId = NormalizeFk(dto.TerritoryId);
            }
        }

        private static int? NormalizeFk(int? id) => id is > 0 ? id : null;

        private async Task<IActionResult?> ValidateOrganizationMasterIdsAsync(int? industryId, int? employeeCountId, int? territoryId)
        {
            if (industryId is int iid
                && !await _context.Industries.AnyAsync(i => i.Id == iid && i.IsActive))
            {
                return BadRequest($"Industry id {iid} does not exist or is inactive.");
            }

            if (employeeCountId is int eid
                && !await _context.EmployeeCounts.AnyAsync(e => e.Id == eid && e.IsActive))
            {
                return BadRequest($"Employee count id {eid} does not exist or is inactive.");
            }

            if (territoryId is int tid
                && !await _context.Territories.AnyAsync(t => t.Id == tid && t.IsActive))
            {
                return BadRequest($"Territory id {tid} does not exist or is inactive.");
            }

            return null;
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Organizations.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            _context.Organizations.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { deleted = true });
        }
    }
}
