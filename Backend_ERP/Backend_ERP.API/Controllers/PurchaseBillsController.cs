using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/purchase-bills")]
    [ApiController]
    public class PurchaseBillsController : ControllerBase
    {
        private readonly IPurchaseBillService _billService;

        public PurchaseBillsController(IPurchaseBillService billService)
        {
            _billService = billService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<PurchaseBillDto>>> GetPurchaseBills(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? paymentStatus,
            [FromQuery] string? vendorName,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 1000,
            [FromQuery] int? userId = null,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            var result = await _billService.GetPurchaseBillsAsync(new PurchaseBillFilterQueryDto
            {
                Search = search,
                Status = status,
                PaymentStatus = paymentStatus,
                VendorName = vendorName,
                Page = page,
                PageSize = pageSize
            }, cancellationToken);

            return Ok(result);
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<PurchaseBillDashboardDto>> GetDashboard([FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            return Ok(await _billService.GetPurchaseBillDashboardAsync(cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PurchaseBillDto>> GetPurchaseBillById(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            var item = await _billService.GetPurchaseBillByIdAsync(id, cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<PurchaseBillDto>> CreatePurchaseBill([FromBody] PurchaseBillCreateRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var created = await _billService.CreatePurchaseBillAsync(request, ResolveUser(userId), cancellationToken);
            return CreatedAtAction(nameof(GetPurchaseBillById), new { id = created.Id, userId }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<PurchaseBillDto>> UpdatePurchaseBill(int id, [FromBody] PurchaseBillCreateRequestDto request, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _billService.UpdatePurchaseBillAsync(id, request, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeletePurchaseBill(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var ok = await _billService.DeletePurchaseBillAsync(id, ResolveUser(userId), cancellationToken);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/approve")]
        public async Task<ActionResult<PurchaseBillDto>> Approve(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _billService.ApprovePurchaseBillAsync(id, payload?.Remarks ?? string.Empty, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpPost("{id:int}/post")]
        public async Task<ActionResult<PurchaseBillDto>> Post(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _billService.PostPurchaseBillAsync(id, payload?.Remarks ?? string.Empty, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpPost("{id:int}/pay")]
        [HttpPost("{id:int}/record-payment")]
        public async Task<ActionResult<PurchaseBillDto>> Pay(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _billService.PayPurchaseBillAsync(id, payload?.Remarks ?? string.Empty, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpPost("{id:int}/void")]
        public async Task<ActionResult<PurchaseBillDto>> Void(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var updated = await _billService.VoidPurchaseBillAsync(id, payload?.Remarks ?? string.Empty, ResolveUser(userId), cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpPost("{id:int}/duplicate")]
        public async Task<ActionResult<PurchaseBillDto>> Duplicate(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            var dup = await _billService.DuplicatePurchaseBillAsync(id, ResolveUser(userId), cancellationToken);
            return dup is null ? NotFound() : Ok(dup);
        }

        private static string ResolveUser(int? userId) => userId is > 0 ? userId.Value.ToString() : "system";
    }
}
