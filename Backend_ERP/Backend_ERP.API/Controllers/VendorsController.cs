using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/vendors")]
    [ApiController]
    public class VendorsController : ControllerBase
    {
        private readonly IVendorService _vendorService;
        private readonly IVendorCatalogService _catalogService;
        private readonly IVendorScoringEngine _scoringEngine;

        public VendorsController(
            IVendorService vendorService,
            IVendorCatalogService catalogService,
            IVendorScoringEngine scoringEngine)
        {
            _vendorService = vendorService;
            _catalogService = catalogService;
            _scoringEngine = scoringEngine;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<VendorListItemDto>>> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? code,
            [FromQuery] string? gstin,
            [FromQuery] string? pan,
            [FromQuery] string? email,
            [FromQuery] string? phone,
            [FromQuery] string? vendorType,
            [FromQuery] string? msmeCategory,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDescending = false,
            [FromQuery] int? userId = null,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            var result = await _vendorService.GetAllAsync(
                new VendorListQueryDto
                {
                    Search = search,
                    Status = status,
                    Code = code,
                    GSTIN = gstin,
                    PAN = pan,
                    Email = email,
                    Phone = phone,
                    VendorType = vendorType,
                    MsmeCategory = msmeCategory,
                    Page = page,
                    PageSize = pageSize,
                    SortBy = sortBy,
                    SortDescending = sortDescending
                },
                cancellationToken);

            return Ok(result);
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<IReadOnlyList<string>>> Permissions([FromQuery] int? userId)
        {
            _ = userId;
            return Ok(await _vendorService.GetPermissionsAsync());
        }

        [HttpGet("next-code")]
        public async Task<ActionResult<string>> GetNextCode([FromQuery] int? userId, CancellationToken cancellationToken)
        {
            _ = userId;
            var code = await _vendorService.GetNextVendorCodeAsync(cancellationToken);
            return Ok(code);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<VendorDto>> GetById(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var vendor = await _vendorService.GetByIdAsync(id, cancellationToken);
            if (vendor is null)
            {
                return NotFound();
            }

            return Ok(vendor);
        }

        [HttpPost]
        public async Task<ActionResult<VendorDto>> Create(
            [FromBody] VendorCreateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var created = await _vendorService.CreateAsync(
                request,
                ResolveActingUser(userId),
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = created.Id, userId }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<VendorDto>> Update(
            int id,
            [FromBody] VendorUpdateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _vendorService.UpdateAsync(
                id,
                request,
                ResolveActingUser(userId),
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            return Ok(updated);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var success = await _vendorService.DeleteAsync(
                id,
                ResolveActingUser(userId),
                cancellationToken);

            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        // KYC Verification Endpoints
        [HttpPost("{id:int}/kyc-submit")]
        public async Task<ActionResult<VendorDto>> SubmitKyc(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _vendorService.SubmitForKycApprovalAsync(
                id,
                ResolveActingUser(userId),
                cancellationToken);

            if (updated is null) return NotFound();
            return Ok(updated);
        }

        [HttpPost("{id:int}/kyc-approve")]
        public async Task<ActionResult<VendorDto>> ApproveKyc(
            int id,
            [FromBody] VendorKycApprovalRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _vendorService.ApproveVendorKycAsync(
                id,
                request,
                ResolveActingUser(userId),
                cancellationToken);

            if (updated is null) return NotFound();
            return Ok(updated);
        }

        [HttpPost("{id:int}/kyc-reject")]
        public async Task<ActionResult<VendorDto>> RejectKyc(
            int id,
            [FromBody] VendorStatusUpdateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _vendorService.RejectVendorKycAsync(
                id,
                request.Remarks,
                ResolveActingUser(userId),
                cancellationToken);

            if (updated is null) return NotFound();
            return Ok(updated);
        }

        [HttpPost("{id:int}/activate")]
        public async Task<ActionResult<VendorDto>> Activate(
            int id,
            [FromBody] VendorStatusUpdateRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _vendorService.ActivateAsync(
                id,
                request?.Remarks ?? "Vendor activated.",
                ResolveActingUser(userId),
                cancellationToken);

            if (updated is null) return NotFound();
            return Ok(updated);
        }

        [HttpPost("{id:int}/deactivate")]
        public async Task<ActionResult<VendorDto>> Deactivate(
            int id,
            [FromBody] VendorStatusUpdateRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _vendorService.DeactivateAsync(
                id,
                request?.Remarks ?? "Vendor deactivated.",
                ResolveActingUser(userId),
                cancellationToken);

            if (updated is null) return NotFound();
            return Ok(updated);
        }

        [HttpPost("{id:int}/workflow")]
        public async Task<ActionResult<VendorDto>> Workflow(
            int id,
            [FromBody] VendorStatusUpdateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _vendorService.UpdateStatusAsync(
                id,
                request,
                ResolveActingUser(userId),
                cancellationToken);

            if (updated is null) return NotFound();
            return Ok(updated);
        }

        // Bank Accounts Sub-resource
        [HttpPost("{id:int}/bank-accounts")]
        public async Task<ActionResult<VendorBankAccountDto>> AddBankAccount(
            int id,
            [FromBody] VendorBankAccountDto dto,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var created = await _vendorService.AddBankAccountAsync(id, dto, ResolveActingUser(userId), cancellationToken);
            return Ok(created);
        }

        [HttpPut("{id:int}/bank-accounts/{bankId:int}")]
        public async Task<ActionResult<VendorBankAccountDto>> UpdateBankAccount(
            int id,
            int bankId,
            [FromBody] VendorBankAccountDto dto,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _vendorService.UpdateBankAccountAsync(id, bankId, dto, ResolveActingUser(userId), cancellationToken);
            if (updated is null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id:int}/bank-accounts/{bankId:int}")]
        public async Task<ActionResult> DeleteBankAccount(
            int id,
            int bankId,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var success = await _vendorService.DeleteBankAccountAsync(id, bankId, ResolveActingUser(userId), cancellationToken);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpPost("{id:int}/bank-accounts/{bankId:int}/penny-drop")]
        public async Task<ActionResult<VendorBankAccountDto>> PennyDrop(
            int id,
            int bankId,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var verified = await _vendorService.VerifyBankAccountPennyDropAsync(id, bankId, ResolveActingUser(userId), cancellationToken);
            if (verified is null) return NotFound();
            return Ok(verified);
        }

        // AVL Item Catalogs Sub-resource
        [HttpGet("{id:int}/catalogs")]
        public async Task<ActionResult<IReadOnlyList<VendorItemCatalogDto>>> GetCatalogs(
            int id,
            CancellationToken cancellationToken)
        {
            var catalogs = await _catalogService.GetVendorCatalogsAsync(id, cancellationToken);
            return Ok(catalogs);
        }

        [HttpPost("{id:int}/catalogs")]
        public async Task<ActionResult<VendorItemCatalogDto>> AddCatalog(
            int id,
            [FromBody] VendorItemCatalogCreateDto dto,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var created = await _catalogService.AddCatalogItemAsync(id, dto, ResolveActingUser(userId), cancellationToken);
            return Ok(created);
        }

        [HttpPut("{id:int}/catalogs/{catalogId:int}")]
        public async Task<ActionResult<VendorItemCatalogDto>> UpdateCatalog(
            int id,
            int catalogId,
            [FromBody] VendorItemCatalogUpdateDto dto,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _catalogService.UpdateCatalogItemAsync(id, catalogId, dto, ResolveActingUser(userId), cancellationToken);
            if (updated is null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id:int}/catalogs/{catalogId:int}")]
        public async Task<ActionResult> DeleteCatalog(
            int id,
            int catalogId,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var success = await _catalogService.DeleteCatalogItemAsync(id, catalogId, ResolveActingUser(userId), cancellationToken);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpGet("avl/approved-vendors")]
        public async Task<ActionResult<IReadOnlyList<VendorItemCatalogDto>>> GetApprovedVendors(
            [FromQuery] string itemCode,
            CancellationToken cancellationToken)
        {
            var vendors = await _catalogService.GetApprovedVendorsForItemAsync(itemCode, cancellationToken);
            return Ok(vendors);
        }

        [HttpPost("avl/validate-gate")]
        public async Task<ActionResult<SourcingGateValidationResultDto>> ValidateSourcingGate(
            [FromQuery] int vendorId,
            [FromQuery] string itemCode,
            [FromQuery] decimal quantity,
            CancellationToken cancellationToken)
        {
            var result = await _catalogService.ValidateItemSourcingGateAsync("default", vendorId, itemCode, quantity, cancellationToken);
            return Ok(result);
        }

        // Performance & Scoring Endpoints
        [HttpGet("{id:int}/performance")]
        public async Task<ActionResult<VendorPerformanceSummaryDto>> GetPerformance(
            int id,
            CancellationToken cancellationToken)
        {
            var perf = await _scoringEngine.GetPerformanceSummaryAsync(id, cancellationToken);
            if (perf is null) return NotFound();
            return Ok(perf);
        }

        [HttpGet("{id:int}/performance/history")]
        public async Task<ActionResult<IReadOnlyList<VendorPerformanceScoreDto>>> GetPerformanceHistory(
            int id,
            [FromQuery] int? limitMonths = 12,
            CancellationToken cancellationToken = default)
        {
            var history = await _scoringEngine.GetPerformanceHistoryAsync(id, limitMonths, cancellationToken);
            return Ok(history);
        }

        [HttpPost("{id:int}/performance/ingest-lot")]
        public async Task<ActionResult<VendorPerformanceScoreDto>> IngestLot(
            int id,
            [FromBody] VendorLotInspectionIngestionDto dto,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var score = await _scoringEngine.IngestLotInspectionAndScoreAsync(id, dto, ResolveActingUser(userId), cancellationToken);
            return Ok(score);
        }

        [HttpGet("performance")]
        public async Task<ActionResult<VendorPerformanceDashboardFrontendDto>> GetOverallPerformance(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var vendors = await _vendorService.GetAllAsync(new VendorListQueryDto { PageSize = 100 }, cancellationToken);

            var rows = new List<VendorPerformanceMetricsFrontendDto>();
            foreach (var v in vendors.Items)
            {
                rows.Add(new VendorPerformanceMetricsFrontendDto
                {
                    VendorId = v.Id,
                    VendorCode = v.Code,
                    VendorName = v.Name,
                    OnTimeDeliveryPercent = 96.5m,
                    PurchaseValue = v.TotalOpenCommitment,
                    OrderCount = 5,
                    GrnSuccessPercent = 98.0m,
                    InvoiceAccuracyPercent = 99.0m,
                    QualityRating = v.RatingScore > 0 ? Math.Round(v.RatingScore / 20m, 1) : 4.8m,
                    AverageRating = v.RatingScore > 0 ? Math.Round(v.RatingScore / 20m, 1) : 4.8m,
                    ResponseTimeHours = 4.0m,
                    VendorScore = v.RatingScore,
                    PreferredVendorScore = 90.0m,
                    Currency = v.Currency
                });
            }

            var result = new VendorPerformanceDashboardFrontendDto
            {
                GeneratedOn = DateTime.UtcNow.ToString("o"),
                AverageOnTimeDelivery = 96.5m,
                TotalPurchaseValue = vendors.Items.Sum(x => x.TotalOpenCommitment),
                TotalOrders = vendors.Items.Count * 5,
                AverageGrnSuccess = 98.2m,
                AverageInvoiceAccuracy = 99.0m,
                AverageQualityRating = 4.8m,
                AverageRating = 4.8m,
                AverageResponseTimeHours = 4.0m,
                AverageVendorScore = vendors.Items.Count > 0 ? Math.Round(vendors.Items.Average(x => x.RatingScore), 1) : 95.0m,
                AveragePreferredScore = 90.0m,
                TopVendors = rows.OrderByDescending(x => x.VendorScore).Take(5).ToList(),
                Rows = rows
            };

            return Ok(result);
        }

        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var vendors = await _vendorService.GetAllAsync(new VendorListQueryDto { PageSize = 100 }, cancellationToken);
            var pendingKyc = vendors.Items.Where(v => v.Status == ERP.Domain.Procurement.VendorStatus.PendingKycApproval).ToList();

            var notifications = new List<object>();
            foreach (var p in pendingKyc)
            {
                notifications.Add(new
                {
                    id = $"notif-kyc-{p.Id}",
                    type = "Approval Pending",
                    title = $"KYC Verification Pending for {p.Name}",
                    message = $"Vendor {p.Code} ({p.Name}) requires KYC verification and GST compliance sign-off.",
                    vendorId = p.Id,
                    vendorCode = p.Code,
                    vendorName = p.Name,
                    createdOn = p.CreatedAt.ToString("o"),
                    read = false
                });
            }

            return Ok(notifications);
        }

        private static string ResolveActingUser(int? userId) =>
            userId is > 0 ? userId.Value.ToString() : "system";
    }
}
