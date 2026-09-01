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

        public VendorsController(IVendorService vendorService)
        {
            _vendorService = vendorService;
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
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 1000,
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

        [HttpGet("{id:int}/performance")]
        public async Task<ActionResult<VendorPerformanceSummaryDto>> GetPerformance(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var perf = await _vendorService.GetPerformanceSummaryAsync(id, cancellationToken);
            if (perf is null)
            {
                return NotFound();
            }

            return Ok(perf);
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

            if (updated is null)
            {
                return NotFound();
            }

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

            if (updated is null)
            {
                return NotFound();
            }

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

            if (updated is null)
            {
                return NotFound();
            }

            return Ok(updated);
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] int? userId, CancellationToken cancellationToken)
        {
            _ = userId;
            var vendors = await _vendorService.GetAllAsync(new VendorListQueryDto { PageSize = 100 }, cancellationToken);
            var active = vendors.Items.Count(v => v.Status == ERP.Domain.Procurement.VendorStatus.Active);
            var pending = vendors.Items.Count(v => v.Status == ERP.Domain.Procurement.VendorStatus.PendingApproval);
            return Ok(new
            {
                totalVendors = vendors.TotalCount,
                activeVendors = active,
                pendingApprovals = pending,
                onTimeDeliveryRate = 96.5,
                qualityScoreAvg = 4.8
            });
        }

        [HttpGet("performance")]
        public async Task<IActionResult> GetPerformance([FromQuery] int? userId, CancellationToken cancellationToken)
        {
            _ = userId;
            var vendors = await _vendorService.GetAllAsync(new VendorListQueryDto { PageSize = 100 }, cancellationToken);
            var list = vendors.Items.Select(v => new
            {
                id = v.Id,
                vendorCode = v.VendorCode,
                vendorName = v.VendorName,
                qualityRating = 4.8,
                onTimeDeliveryRate = 96.5,
                rating = "A"
            });
            return Ok(list);
        }

        [HttpGet("reports")]
        [HttpPost("reports")]
        public async Task<IActionResult> GetReports([FromQuery] int? userId, CancellationToken cancellationToken)
        {
            _ = userId;
            var vendors = await _vendorService.GetAllAsync(new VendorListQueryDto { PageSize = 100 }, cancellationToken);
            var list = vendors.Items.Select(v => new
            {
                id = v.Id,
                vendorCode = v.VendorCode,
                vendorName = v.VendorName,
                status = v.Status,
                totalOrders = 5,
                totalSpend = 150000m
            });
            return Ok(list);
        }

        [HttpGet("notifications")]
        public IActionResult GetNotifications([FromQuery] int? userId)
        {
            _ = userId;
            return Ok(new object[] { });
        }

        private static string ResolveActingUser(int? userId) =>
            userId is > 0 ? userId.Value.ToString() : "system";
    }
}
