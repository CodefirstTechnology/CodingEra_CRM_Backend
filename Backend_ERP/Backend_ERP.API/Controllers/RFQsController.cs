using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/rfqs")]
    [ApiController]
    public class RFQsController : ControllerBase
    {
        private readonly IRFQService _rfqService;

        public RFQsController(IRFQService rfqService)
        {
            _rfqService = rfqService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<RFQListItemDto>>> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? prNumber,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDescending = false,
            [FromQuery] int? userId = null,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            var result = await _rfqService.GetAllAsync(
                new RFQListQueryDto
                {
                    Search = search,
                    Status = status,
                    PRNumber = prNumber,
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
            var number = await _rfqService.GetNextRFQNumberAsync(cancellationToken);
            return Ok(number);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<RFQDto>> GetById(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var rfq = await _rfqService.GetByIdAsync(id, cancellationToken);
            if (rfq is null)
            {
                return NotFound();
            }

            return Ok(rfq);
        }

        [HttpPost]
        public async Task<ActionResult<RFQDto>> Create(
            [FromBody] RFQCreateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var created = await _rfqService.CreateAsync(
                request,
                ResolveActingUser(userId),
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = created.Id, userId }, created);
        }

        [HttpPost("create-from-pr/{prId:int}")]
        public async Task<ActionResult<RFQDto>> CreateFromPR(
            int prId,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var created = await _rfqService.CreateFromPRAsync(
                prId,
                ResolveActingUser(userId),
                cancellationToken);

            if (created is null)
            {
                return NotFound();
            }

            return CreatedAtAction(nameof(GetById), new { id = created.Id, userId }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<RFQDto>> Update(
            int id,
            [FromBody] RFQUpdateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _rfqService.UpdateAsync(
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
            var success = await _rfqService.DeleteAsync(
                id,
                ResolveActingUser(userId),
                cancellationToken);

            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpPost("{id:int}/send")]
        public async Task<ActionResult<RFQDto>> Send(
            int id,
            [FromBody] RFQStatusUpdateRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _rfqService.SendAsync(
                id,
                request?.Remarks ?? "RFQ published and sent to selected vendors.",
                ResolveActingUser(userId),
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            return Ok(updated);
        }

        [HttpPost("{id:int}/close")]
        public async Task<ActionResult<RFQDto>> Close(
            int id,
            [FromBody] RFQStatusUpdateRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _rfqService.CloseAsync(
                id,
                request?.Remarks ?? "RFQ closed.",
                ResolveActingUser(userId),
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            return Ok(updated);
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<ActionResult<RFQDto>> Cancel(
            int id,
            [FromBody] RFQStatusUpdateRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var updated = await _rfqService.CancelAsync(
                id,
                request?.Remarks ?? "RFQ cancelled.",
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
