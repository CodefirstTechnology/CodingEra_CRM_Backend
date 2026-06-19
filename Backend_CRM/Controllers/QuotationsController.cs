using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using CRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers
{
    [Route("api/quotations")]
    [ApiController]
    public class QuotationsController : ControllerBase
    {
        private readonly TaskDbcontext _context;
        private readonly IQuotationService _quotationService;
        private readonly IUserTargetService _userTargets;

        public QuotationsController(
            TaskDbcontext context,
            IQuotationService quotationService,
            IUserTargetService userTargets)
        {
            _context = context;
            _quotationService = quotationService;
            _userTargets = userTargets;
        }

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings([FromQuery] int userId)
        {
            _ = userId;
            return Ok(await _quotationService.GetSettingsAsync());
        }

        [HttpPut("settings")]
        public async Task<IActionResult> UpdateSettings([FromQuery] int userId, [FromBody] QuotationSettingsDto dto)
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
            return Ok(await _quotationService.UpdateSettingsAsync(dto));
        }

        [HttpGet("next-number")]
        public async Task<IActionResult> GetNextNumber(
            [FromQuery] int userId,
            [FromQuery] string? companyCode = null,
            [FromQuery] DateTime? asOf = null)
        {
            _ = userId;
            return Ok(await _quotationService.PeekNextNumberAsync(companyCode, asOf));
        }

        [HttpGet("statuses")]
        public IActionResult GetStatuses([FromQuery] int userId)
        {
            _ = userId;
            return Ok(QuotationStatuses.All);
        }

        [HttpGet("item-grid/columns")]
        public async Task<IActionResult> GetItemGridColumns([FromQuery] int userId)
        {
            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null)
            {
                return auditErr;
            }

            return Ok(await _quotationService.GetGridColumnsForUserAsync(userId));
        }

        [HttpPut("item-grid/columns")]
        public async Task<IActionResult> SaveItemGridColumns(
            [FromQuery] int userId,
            [FromBody] QuotationGridColumnsDto dto)
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

            return Ok(await _quotationService.SaveUserGridColumnsAsync(userId, dto));
        }

        [HttpGet("item-grid/defaults")]
        public async Task<IActionResult> GetItemGridDefaults([FromQuery] int userId)
        {
            _ = userId;
            return Ok(await _quotationService.GetGridDefaultColumnsAsync());
        }

        [HttpPut("item-grid/defaults")]
        public async Task<IActionResult> SaveItemGridDefaults(
            [FromQuery] int userId,
            [FromBody] QuotationGridColumnsDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var adminErr = await AdminUserValidation.ValidateAdminUserAsync(_context, userId);
            if (adminErr != null)
            {
                return adminErr;
            }

            return Ok(await _quotationService.SaveGridDefaultColumnsAsync(userId, dto));
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int userId,
            [FromQuery] string? status = null,
            [FromQuery] int? dealId = null,
            [FromQuery] string? search = null)
        {
            var (ctx, ctxErr) = await QuotationAccessHelper.ResolveUserContextAsync(_context, userId, Request);
            if (ctxErr != null)
            {
                return ctxErr;
            }

            IQueryable<Quotation> q = QuotationAccessHelper.ApplyVisibilityFilter(
                _context.Quotations.AsNoTracking(),
                ctx!);

            if (!string.IsNullOrWhiteSpace(status))
            {
                var st = status.Trim();
                q = q.Where(x => x.Status == st);
            }

            if (dealId is > 0)
            {
                q = q.Where(x => x.DealId == dealId);
            }

            q = QuotationAccessHelper.ApplySearchFilter(q, search);

            var includeCreator = ctx!.CanViewAll;

            var rows = await q
                .OrderByDescending(x => x.UpdatedAt)
                .Select(x => new
                {
                    x.Id,
                    x.DealId,
                    DealStatus = x.DealId != null
                        ? _context.Deals
                            .Where(d => d.Id == x.DealId)
                            .Select(d => d.Status)
                            .FirstOrDefault()
                        : null,
                    x.CustomerName,
                    x.CompanyName,
                    x.ContactPerson,
                    x.MobileNumber,
                    x.EmailAddress,
                    x.ReferenceNumber,
                    x.QuotationNumber,
                    x.QuotationDate,
                    x.Status,
                    x.GrandTotal,
                    x.CreatedBy,
                    CreatedByName = includeCreator
                        ? _context.Users
                            .Where(u => u.Id == x.CreatedBy)
                            .Select(u => u.FullName)
                            .FirstOrDefault()
                        : null,
                    x.CreatedAt,
                    x.UpdatedAt,
                })
                .ToListAsync();

            var pipeline = await QuotationDealLockHelper.LoadActivePipelineAsync(_context);
            var list = rows.Select(x => new
            {
                x.Id,
                x.DealId,
                DealClosed = x.DealId is > 0
                    && !string.IsNullOrWhiteSpace(x.DealStatus)
                    && DealStageValidationHelper.IsClosed(x.DealStatus, pipeline),
                x.CustomerName,
                x.CompanyName,
                x.ContactPerson,
                x.MobileNumber,
                x.EmailAddress,
                x.ReferenceNumber,
                x.QuotationNumber,
                x.QuotationDate,
                x.Status,
                x.GrandTotal,
                x.CreatedBy,
                x.CreatedByName,
                x.CreatedAt,
                x.UpdatedAt,
            });

            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, [FromQuery] int userId)
        {
            var (ctx, ctxErr) = await QuotationAccessHelper.ResolveUserContextAsync(_context, userId, Request);
            if (ctxErr != null)
            {
                return ctxErr;
            }

            var accessErr = await QuotationAccessHelper.EnsureCanAccessAsync(_context, id, ctx!);
            if (accessErr != null)
            {
                return accessErr;
            }

            var q = await LoadQuotationAsync(id);
            if (q == null)
            {
                return NotFound();
            }

            var response = QuotationMappingHelper.ToUpsertDto(q);
            response.DealClosed = await QuotationDealLockHelper.IsDealClosedAsync(_context, response.DealId);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] int userId, [FromBody] QuotationUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var validation = ValidateRequired(dto);
            if (validation != null)
            {
                return validation;
            }

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null)
            {
                return auditErr;
            }

            AuditUserValidation.SetAuditUser(_context, userId);

            var generationErr = await EnsureLinkedDealAllowsQuotationGenerationAsync(dto.DealId);
            if (generationErr != null)
            {
                return generationErr;
            }

            var entity = new Quotation { Id = 0 };
            QuotationMappingHelper.ApplyHeader(entity, dto);
            if (string.IsNullOrWhiteSpace(entity.Status))
            {
                entity.Status = QuotationStatuses.Draft;
            }

            entity.QuotationDate = dto.QuotationDate.HasValue
                ? DateTimeUtcHelper.ToUtc(dto.QuotationDate.Value)
                : DateTime.UtcNow;
            var lines = QuotationMappingHelper.MapLineItems(0, dto.LineItems);
            var customCharges = QuotationMappingHelper.MapCustomCharges(0, dto.CustomCharges);
            entity.LineItems = lines;
            entity.AdditionalCharges = customCharges;
            QuotationMappingHelper.ApplyTotals(entity, lines);

            // Always assign on create — the UI preview number is not reserved until save.
            await _quotationService.ReserveNextNumberAsync(entity, forceNewSequence: true);

            await _context.Quotations.AddAsync(entity);
            await QuotationDealLockHelper.SyncDealAmountFromGrandTotalAsync(
                _context,
                entity.DealId,
                entity.GrandTotal);
            await _context.SaveChangesAsync();
            if (entity.DealId is > 0)
            {
                await _userTargets.RecalculateForDealAsync(entity.DealId.Value);
            }

            var saved = await LoadQuotationAsync(entity.Id);
            return Ok(QuotationMappingHelper.ToUpsertDto(saved!));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromQuery] int userId, [FromBody] QuotationUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var validation = ValidateRequired(dto);
            if (validation != null)
            {
                return validation;
            }

            var (ctx, ctxErr) = await QuotationAccessHelper.ResolveUserContextAsync(_context, userId, Request);
            if (ctxErr != null)
            {
                return ctxErr;
            }

            var accessErr = await QuotationAccessHelper.EnsureCanAccessAsync(_context, id, ctx!);
            if (accessErr != null)
            {
                return accessErr;
            }

            AuditUserValidation.SetAuditUser(_context, userId);

            if (dto.Id != 0 && dto.Id != id)
            {
                return BadRequest("Route id and body id must match when the body includes an id.");
            }

            var existing = await _context.Quotations
                .Include(x => x.LineItems)
                .Include(x => x.AdditionalCharges)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null)
            {
                return NotFound();
            }

            var closedDealErr = await EnsureLinkedDealAllowsQuotationEditAsync(existing.DealId);
            if (closedDealErr != null)
            {
                return closedDealErr;
            }

            QuotationMappingHelper.ApplyHeader(existing, dto);
            if (dto.QuotationDate.HasValue)
            {
                existing.QuotationDate = DateTimeUtcHelper.ToUtc(dto.QuotationDate.Value);
            }

            _context.QuotationLineItems.RemoveRange(existing.LineItems);
            _context.QuotationAdditionalCharges.RemoveRange(existing.AdditionalCharges);
            var lines = QuotationMappingHelper.MapLineItems(id, dto.LineItems);
            var customCharges = QuotationMappingHelper.MapCustomCharges(id, dto.CustomCharges);
            foreach (var line in lines)
            {
                line.QuotationId = id;
                await _context.QuotationLineItems.AddAsync(line);
            }

            foreach (var charge in customCharges)
            {
                charge.QuotationId = id;
                await _context.QuotationAdditionalCharges.AddAsync(charge);
            }

            existing.LineItems = lines;
            existing.AdditionalCharges = customCharges;
            QuotationMappingHelper.ApplyTotals(existing, lines);

            await QuotationDealLockHelper.SyncDealAmountFromGrandTotalAsync(
                _context,
                existing.DealId,
                existing.GrandTotal);
            await _context.SaveChangesAsync();
            if (existing.DealId is > 0)
            {
                await _userTargets.RecalculateForDealAsync(existing.DealId.Value);
            }

            var saved = await LoadQuotationAsync(id);
            return Ok(QuotationMappingHelper.ToUpsertDto(saved!));
        }

        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> PatchStatus(
            int id,
            [FromQuery] int userId,
            [FromBody] QuotationStatusPatchDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Status))
            {
                return BadRequest("status is required.");
            }

            var (ctx, ctxErr) = await QuotationAccessHelper.ResolveUserContextAsync(_context, userId, Request);
            if (ctxErr != null)
            {
                return ctxErr;
            }

            var accessErr = await QuotationAccessHelper.EnsureCanAccessAsync(_context, id, ctx!);
            if (accessErr != null)
            {
                return accessErr;
            }

            AuditUserValidation.SetAuditUser(_context, userId);

            var existing = await _context.Quotations.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            var closedDealErr = await EnsureLinkedDealAllowsQuotationEditAsync(existing.DealId);
            if (closedDealErr != null)
            {
                return closedDealErr;
            }

            var st = dto.Status.Trim();
            if (!QuotationStatuses.All.Contains(st, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest($"Invalid status. Allowed: {string.Join(", ", QuotationStatuses.All)}");
            }

            existing.Status = QuotationStatuses.All.First(s =>
                s.Equals(st, StringComparison.OrdinalIgnoreCase));
            await _context.SaveChangesAsync();

            var saved = await LoadQuotationAsync(id);
            return Ok(QuotationMappingHelper.ToUpsertDto(saved!));
        }

        [HttpPost("{id:int}/duplicate")]
        public async Task<IActionResult> Duplicate(int id, [FromQuery] int userId)
        {
            var (ctx, ctxErr) = await QuotationAccessHelper.ResolveUserContextAsync(_context, userId, Request);
            if (ctxErr != null)
            {
                return ctxErr;
            }

            var accessErr = await QuotationAccessHelper.EnsureCanAccessAsync(_context, id, ctx!);
            if (accessErr != null)
            {
                return accessErr;
            }

            AuditUserValidation.SetAuditUser(_context, userId);

            var source = await LoadQuotationAsync(id);
            if (source == null)
            {
                return NotFound();
            }

            var dto = QuotationMappingHelper.ToUpsertDto(source);
            if (await QuotationDealLockHelper.IsDealClosedAsync(_context, source.DealId))
            {
                dto.DealId = null;
            }

            dto.Id = 0;
            dto.Status = QuotationStatuses.Draft;
            dto.QuotationNumber = string.Empty;
            dto.SequenceNumber = 0;
            dto.QuotationDate = DateTime.UtcNow;
            dto.FiscalYearLabel = QuotationNumberHelper.FiscalYearLabelFor(dto.QuotationDate.Value);
            foreach (var line in dto.LineItems)
            {
                line.Id = 0;
            }

            foreach (var charge in dto.CustomCharges)
            {
                charge.Id = 0;
            }

            var entity = new Quotation { Id = 0 };
            QuotationMappingHelper.ApplyHeader(entity, dto);
            entity.QuotationDate = dto.QuotationDate.HasValue
                ? DateTimeUtcHelper.ToUtc(dto.QuotationDate.Value)
                : DateTime.UtcNow;
            var lines = QuotationMappingHelper.MapLineItems(0, dto.LineItems);
            var customCharges = QuotationMappingHelper.MapCustomCharges(0, dto.CustomCharges);
            entity.LineItems = lines;
            entity.AdditionalCharges = customCharges;
            QuotationMappingHelper.ApplyTotals(entity, lines);
            await _quotationService.ReserveNextNumberAsync(entity, forceNewSequence: true);

            await _context.Quotations.AddAsync(entity);
            await _context.SaveChangesAsync();

            var saved = await LoadQuotationAsync(entity.Id);
            return Ok(QuotationMappingHelper.ToUpsertDto(saved!));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] int userId)
        {
            var (ctx, ctxErr) = await QuotationAccessHelper.ResolveUserContextAsync(_context, userId, Request);
            if (ctxErr != null)
            {
                return ctxErr;
            }

            var accessErr = await QuotationAccessHelper.EnsureCanAccessAsync(_context, id, ctx!);
            if (accessErr != null)
            {
                return accessErr;
            }

            var existing = await _context.Quotations
                .Include(x => x.LineItems)
                .Include(x => x.AdditionalCharges)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null)
            {
                return NotFound();
            }

            var closedDealErr = await EnsureLinkedDealAllowsQuotationEditAsync(existing.DealId);
            if (closedDealErr != null)
            {
                return closedDealErr;
            }

            _context.QuotationLineItems.RemoveRange(existing.LineItems);
            _context.QuotationAdditionalCharges.RemoveRange(existing.AdditionalCharges);
            _context.Quotations.Remove(existing);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Deleted successfully" });
        }

        private async Task<IActionResult?> EnsureLinkedDealAllowsQuotationEditAsync(int? dealId)
        {
            if (await QuotationDealLockHelper.IsDealClosedAsync(_context, dealId))
            {
                return BadRequest(QuotationDealLockHelper.ClosedDealMessage);
            }

            return null;
        }

        private async Task<IActionResult?> EnsureLinkedDealAllowsQuotationGenerationAsync(int? dealId)
        {
            if (await QuotationDealLockHelper.IsQuotationGenerationBlockedAsync(_context, dealId))
            {
                return BadRequest(QuotationDealLockHelper.GenerationBlockedMessage);
            }

            return null;
        }

        private async Task<Quotation?> LoadQuotationAsync(int id) =>
            await _context.Quotations.AsNoTracking()
                .Include(x => x.LineItems.OrderBy(l => l.LineIndex))
                .Include(x => x.AdditionalCharges.OrderBy(c => c.SortIndex))
                .FirstOrDefaultAsync(x => x.Id == id);

        private static BadRequestObjectResult? ValidateRequired(QuotationUpsertDto dto)
        {
            var errors = new List<string>();
            var hasName =
                !string.IsNullOrWhiteSpace(dto.FirstName) && !string.IsNullOrWhiteSpace(dto.LastName)
                || !string.IsNullOrWhiteSpace(dto.CustomerName);
            if (!hasName)
            {
                errors.Add("First name and last name (or customer name) are required.");
            }

            if (string.IsNullOrWhiteSpace(dto.CompanyName))
            {
                errors.Add("Organization / company name is required.");
            }

            if (errors.Count == 0)
            {
                return null;
            }

            return new BadRequestObjectResult(new { message = string.Join(" ", errors), errors });
        }
    }
}
