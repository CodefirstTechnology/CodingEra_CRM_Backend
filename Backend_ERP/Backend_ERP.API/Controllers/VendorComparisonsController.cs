using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/vendor-comparisons")]
    [ApiController]
    public class VendorComparisonsController : ControllerBase
    {
        private readonly IVendorComparisonService _vcService;

        public VendorComparisonsController(IVendorComparisonService vcService)
        {
            _vcService = vcService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<VendorComparisonListItemDto>>> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDescending = false,
            [FromQuery] int? userId = null,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            var result = await _vcService.GetAllAsync(
                new VendorComparisonListQueryDto
                {
                    Search = search,
                    Status = status,
                    Page = page,
                    PageSize = pageSize,
                    SortBy = sortBy,
                    SortDescending = sortDescending
                },
                cancellationToken);

            return Ok(result);
        }

        [HttpGet("next-number")]
        public async Task<ActionResult<string>> GetNextNumber([FromQuery] int? userId, CancellationToken cancellationToken)
        {
            _ = userId;
            var number = await _vcService.GetNextComparisonNumberAsync(cancellationToken);
            return Ok(number);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<VendorComparisonDto>> GetById(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var vc = await _vcService.GetByIdAsync(id, cancellationToken);
            if (vc is null)
            {
                return NotFound();
            }

            return Ok(vc);
        }

        [HttpPost]
        public async Task<ActionResult<VendorComparisonDto>> Create(
            [FromBody] VendorComparisonCreateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var created = await _vcService.CreateAsync(
                request,
                ResolveActingUser(userId),
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = created.Id, userId }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<VendorComparisonDto>> Update(
            int id,
            [FromBody] VendorComparisonUpdateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _vcService.UpdateAsync(
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
            var success = await _vcService.DeleteAsync(
                id,
                ResolveActingUser(userId),
                cancellationToken);

            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpPost("{id:int}/compared")]
        public async Task<ActionResult<VendorComparisonDto>> MarkCompared(
            int id,
            [FromBody] VendorComparisonStatusUpdateRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _vcService.MarkComparedAsync(
                id,
                request?.Remarks ?? "Vendor comparison evaluation completed.",
                ResolveActingUser(userId),
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            return Ok(updated);
        }

        [HttpPost("{id:int}/approve")]
        public async Task<ActionResult<VendorComparisonDto>> Approve(
            int id,
            [FromBody] VendorComparisonStatusUpdateRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _vcService.ApproveAsync(
                id,
                request?.Remarks ?? "Vendor comparison approved.",
                ResolveActingUser(userId),
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            return Ok(updated);
        }

        [HttpPost("{id:int}/award")]
        public async Task<ActionResult<VendorComparisonDto>> Award(
            int id,
            [FromBody] VendorAwardRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _vcService.AwardVendorAsync(
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

        private static string ResolveActingUser(int? userId) =>
            userId is > 0 ? userId.Value.ToString() : "system";
    }
}
