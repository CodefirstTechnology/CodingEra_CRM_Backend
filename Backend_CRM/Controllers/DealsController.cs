using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers
{
    [Route("api/deals")]
    [ApiController]
    public class DealsController : ControllerBase
    {
        private readonly TaskDbcontext _context;

        public DealsController(TaskDbcontext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int userId,
            [FromQuery] string? status = null,
            [FromQuery] int? statusId = null)
        {
            _ = userId;
            IQueryable<Deal> q = _context.Deals.AsNoTracking().Include(d => d.DealStatus);

            if (statusId is > 0)
            {
                q = q.Where(d => d.DealStatusId == statusId);
            }
            else if (!string.IsNullOrWhiteSpace(status))
            {
                var st = status.Trim();
                q = q.Where(d =>
                    d.Status == st
                    || (d.DealStatus != null && d.DealStatus.Name == st));
            }

            return Ok(await q.OrderByDescending(d => d.LastModified).ToListAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, [FromQuery] int userId)
        {
            _ = userId;
            var d = await _context.Deals.AsNoTracking()
                .Include(x => x.DealStatus)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (d == null)
            {
                return NotFound();
            }

            return Ok(d);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] int userId, [FromBody] DealUpsertDto dto)
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

            var entity = CrmWriteMappings.ToDeal(dto, 0);
            entity.Id = 0;

            var statusErr = await ApplyDealStatusFromDtoAsync(dto, entity, isCreate: true);
            if (statusErr != null)
            {
                return statusErr;
            }

            await _context.Deals.AddAsync(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromQuery] int userId, [FromBody] DealUpsertDto dto)
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

            var existing = await _context.Deals.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            CrmWriteMappings.Apply(existing, dto);

            var statusErr = await ApplyDealStatusFromDtoAsync(dto, existing, isCreate: false);
            if (statusErr != null)
            {
                return statusErr;
            }

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        /// <summary>Update deal pipeline status (DEAL ASSIGNMENT dropdown).</summary>
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromQuery] int userId,
            [FromBody] DealStatusUpdateDto dto)
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

            var existing = await _context.Deals.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            var statusErr = await ApplyDealStatusFromDtoAsync(
                new DealUpsertDto
                {
                    DealStatusId = dto.DealStatusId,
                    Status = dto.Status ?? string.Empty,
                },
                existing,
                isCreate: false,
                requireStatus: true);
            if (statusErr != null)
            {
                return statusErr;
            }

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Deals.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            _context.Deals.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { deleted = true });
        }

        private async Task<IActionResult?> ApplyDealStatusFromDtoAsync(
            DealUpsertDto dto,
            Deal deal,
            bool isCreate,
            bool requireStatus = false)
        {
            if (dto.DealStatusId is int dsid && dsid > 0)
            {
                var status = await _context.DealStatuses.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == dsid && s.IsActive);
                if (status == null)
                {
                    return BadRequest($"Deal status id {dsid} does not exist or is inactive.");
                }

                deal.DealStatusId = dsid;
                deal.Status = status.Name;
                return null;
            }

            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                var statusId = await ResolveNameToIdAsync(_context.DealStatuses, dto.Status, requireActive: true);
                if (statusId == null)
                {
                    return BadRequest($"Deal status '{dto.Status.Trim()}' does not exist or is inactive.");
                }

                deal.DealStatusId = statusId;
                deal.Status = dto.Status.Trim();
                return null;
            }

            if (isCreate)
            {
                var defaultStatus = await _context.DealStatuses.AsNoTracking()
                    .Where(s => s.IsActive && s.Name == "Qualification")
                    .Select(s => new { s.Id, s.Name })
                    .FirstOrDefaultAsync()
                    ?? await _context.DealStatuses.AsNoTracking()
                        .Where(s => s.IsActive)
                        .OrderBy(s => s.Id)
                        .Select(s => new { s.Id, s.Name })
                        .FirstOrDefaultAsync();

                if (defaultStatus == null)
                {
                    return BadRequest("No active deal statuses are configured.");
                }

                deal.DealStatusId = defaultStatus.Id;
                deal.Status = defaultStatus.Name;
                return null;
            }

            if (requireStatus)
            {
                return BadRequest("dealStatusId or status is required.");
            }

            return null;
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
    }
}
