using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using CRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers
{
    [Route("api/deals")]
    [ApiController]
    public class DealsController : ControllerBase
    {
        private readonly TaskDbcontext _context;
        private readonly ILogger<DealsController> _logger;
        private readonly IRbacService _rbac;
        private readonly IUserTargetService _userTargets;

        public DealsController(
            TaskDbcontext context,
            ILogger<DealsController> logger,
            IRbacService rbac,
            IUserTargetService userTargets)
        {
            _context = context;
            _logger = logger;
            _rbac = rbac;
            _userTargets = userTargets;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int userId,
            [FromQuery] string? status = null,
            [FromQuery] int? statusId = null)
        {
            _ = userId;
            IQueryable<Deal> q = _context.Deals.AsNoTracking()
                .Include(d => d.DealStatus)
                .Include(d => d.AssignedToUser)
                .Include(d => d.DealOwner);

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

            var deals = await q.OrderByDescending(d => d.LastModified).ToListAsync();
            await DealAmountHelper.ApplyLatestQuotationAmountsAsync(_context, deals);
            return Ok(deals);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, [FromQuery] int userId)
        {
            _ = userId;
            var d = await _context.Deals.AsNoTracking()
                .Include(x => x.DealStatus)
                .Include(x => x.AssignedToUser)
                .Include(x => x.DealOwner)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (d == null)
            {
                return NotFound();
            }

            await DealAmountHelper.ApplyLatestQuotationAmountsAsync(_context, new[] { d });
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

            await RecordOwnershipEnforcement.EnforceDealOwnerOnCreateAsync(_rbac, userId, entity);

            await _context.Deals.AddAsync(entity);
            await _context.SaveChangesAsync();
            await _userTargets.RecalculateForDealAsync(entity.Id);
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

            var previousOwnerId = existing.DealOwnerId;

            var allStatuses = await LoadAllDealStatusesAsync();
            if (DealStageValidationHelper.IsClosed(existing.Status, allStatuses))
            {
                return BadRequest(DealStageValidationHelper.ClosedDealMessage);
            }

            await RecordOwnershipEnforcement.EnforceDealOwnerOnUpdateAsync(_rbac, userId, dto, existing);

            CrmWriteMappings.Apply(existing, dto);

            var statusErr = await ApplyDealStatusFromDtoAsync(dto, existing, isCreate: false);
            if (statusErr != null)
            {
                return statusErr;
            }

            await _context.SaveChangesAsync();
            await _userTargets.RecalculateForDealAsync(id, previousOwnerId);
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

            var stageComment = dto.Comment?.Trim();
            if (!string.IsNullOrWhiteSpace(stageComment))
            {
                _context.DealStageChangeComment = stageComment;
            }

            var existing = await _context.Deals.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            var previousOwnerId = existing.DealOwnerId;

            var lostReason = dto.LostReason?.Trim();
            if (!string.IsNullOrWhiteSpace(lostReason))
            {
                _context.DealStageChangeComment = string.IsNullOrWhiteSpace(stageComment)
                    ? lostReason
                    : $"{stageComment} | Lost reason: {lostReason}";
            }

            var statusErr = await ApplyDealStatusFromDtoAsync(
                new DealUpsertDto
                {
                    DealStatusId = dto.DealStatusId,
                    Status = dto.Status ?? string.Empty,
                    LostReason = lostReason,
                },
                existing,
                isCreate: false,
                requireStatus: true);
            if (statusErr != null)
            {
                return statusErr;
            }

            await _context.SaveChangesAsync();
            await _userTargets.RecalculateForDealAsync(id, previousOwnerId);
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

            var allStatuses = await LoadAllDealStatusesAsync();
            if (DealStageValidationHelper.IsClosed(entity.Status, allStatuses))
            {
                return BadRequest(DealStageValidationHelper.ClosedDealMessage);
            }

            var ownerId = entity.DealOwnerId;
            _context.Deals.Remove(entity);
            await _context.SaveChangesAsync();
            if (ownerId is > 0)
            {
                await _userTargets.RecalculateForUserAsync(ownerId.Value);
            }
            return Ok(new { deleted = true });
        }

        [HttpGet("{id:int}/stage-history")]
        public async Task<IActionResult> GetStageHistory(int id, [FromQuery] int userId)
        {
            _ = userId;
            var exists = await _context.Deals.AsNoTracking().AnyAsync(d => d.Id == id);
            if (!exists)
            {
                return NotFound();
            }

            var rows = await _context.DealStageHistories.AsNoTracking()
                .Where(h => h.DealId == id)
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => new DealStageHistoryDto
                {
                    Id = h.Id,
                    DealId = h.DealId,
                    PreviousStage = h.PreviousStage,
                    NewStage = h.NewStage,
                    ChangedByUserId = h.ChangedByUserId,
                    ChangedAt = h.ChangedAt,
                    Comment = string.IsNullOrWhiteSpace(h.Comment) ? null : h.Comment,
                })
                .ToListAsync();

            return Ok(rows);
        }

        private async Task<IActionResult?> ApplyDealStatusFromDtoAsync(
            DealUpsertDto dto,
            Deal deal,
            bool isCreate,
            bool requireStatus = false)
        {
            var previousStatus = deal.Status;
            string? resolvedStatusName = null;
            int? resolvedStatusId = null;

            if (dto.DealStatusId is int dsid && dsid > 0)
            {
                var statusById = await _context.DealStatuses.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == dsid && s.IsActive);
                if (statusById != null)
                {
                    resolvedStatusId = dsid;
                    resolvedStatusName = statusById.Name;
                }
                else if (!isCreate)
                {
                    return BadRequest($"Deal status id {dsid} does not exist or is inactive.");
                }
            }

            if (resolvedStatusName == null && !string.IsNullOrWhiteSpace(dto.Status))
            {
                var normalized = NormalizeDealStatusName(dto.Status);
                var statusId = await ResolveNameToIdAsync(_context.DealStatuses, normalized, requireActive: true);
                if (statusId != null)
                {
                    resolvedStatusId = statusId;
                    resolvedStatusName = normalized;
                }
                else if (!isCreate)
                {
                    return BadRequest($"Deal status '{dto.Status.Trim()}' does not exist or is inactive.");
                }
            }

            if (resolvedStatusName != null && resolvedStatusId != null)
            {
                if (!isCreate)
                {
                    var historyStages = await _context.DealStageHistories.AsNoTracking()
                        .Where(h => h.DealId == deal.Id)
                        .Select(h => h.NewStage)
                        .ToListAsync();

                    var allStatuses = await LoadAllDealStatusesAsync();
                    var activePipeline = DealStageValidationHelper.OrderPipeline(allStatuses);
                    var validation = DealStageValidationHelper.ValidateTransition(
                        allStatuses,
                        activePipeline,
                        previousStatus,
                        resolvedStatusName,
                        historyStages,
                        dto.LostReason);

                    if (!validation.Allowed)
                    {
                        return BadRequest(validation.Message);
                    }

                    if (DealStageValidationHelper.IsClosedLost(resolvedStatusName, allStatuses)
                        && !string.IsNullOrWhiteSpace(dto.LostReason))
                    {
                        deal.LostReason = dto.LostReason.Trim();
                    }
                }

                deal.DealStatusId = resolvedStatusId;
                deal.Status = resolvedStatusName;
                return null;
            }

            if (isCreate)
            {
                var defaultStatus = await _context.DealStatuses.AsNoTracking()
                    .Where(s => s.IsActive && !s.IsWon && !s.IsLost)
                    .OrderBy(s => s.SortOrder)
                    .ThenBy(s => s.Id)
                    .Select(s => new { s.Id, s.Name })
                    .FirstOrDefaultAsync()
                    ?? await _context.DealStatuses.AsNoTracking()
                        .Where(s => s.IsActive)
                        .OrderBy(s => s.SortOrder)
                        .ThenBy(s => s.Id)
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

        private async Task<IReadOnlyList<DealStatus>> LoadAllDealStatusesAsync() =>
            await _context.DealStatuses.AsNoTracking().ToListAsync();

        private static string NormalizeDealStatusName(string name) => name.Trim();

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
