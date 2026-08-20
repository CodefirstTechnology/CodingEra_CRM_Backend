using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Accounting;
using ERP.Application.Accounting.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [ApiController]
    [Route("api/receipts")]
    public class ReceiptsController : ControllerBase
    {
        private readonly IReceiptService _receiptService;

        public ReceiptsController(IReceiptService receiptService)
        {
            _receiptService = receiptService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ReceiptListItemDto>>> GetReceipts(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int? customerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortDir = null,
            CancellationToken cancellationToken = default)
        {
            var query = new ListQueryDto
            {
                Search = search,
                Status = status,
                CustomerId = customerId,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDir = sortDir
            };

            return Ok(await _receiptService.GetReceiptsAsync(query, cancellationToken));
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<ReceiptDashboardDto>> GetDashboard(CancellationToken cancellationToken = default)
        {
            return Ok(await _receiptService.GetReceiptDashboardAsync(cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ReceiptEntryDto>> GetById(int id, CancellationToken cancellationToken = default)
        {
            var item = await _receiptService.GetReceiptByIdAsync(id, cancellationToken);
            return item is null ? NotFound(new { message = $"Receipt #{id} not found" }) : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<ReceiptEntryDto>> Create([FromBody] ReceiptCreateRequestDto dto, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            try
            {
                var created = await _receiptService.CreateReceiptAsync(dto, "Finance User", cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ReceiptEntryDto>> Update(int id, [FromBody] ReceiptUpdateRequestDto dto, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            try
            {
                var updated = await _receiptService.UpdateReceiptAsync(id, dto, "Finance User", cancellationToken);
                return Ok(updated);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Receipt #{id} not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var deleted = await _receiptService.DeleteReceiptAsync(id, cancellationToken);
                return deleted ? NoContent() : NotFound(new { message = $"Receipt #{id} not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/approve")]
        public async Task<ActionResult<ReceiptEntryDto>> Approve(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            try
            {
                var result = await _receiptService.ApproveReceiptAsync(id, payload, "Finance User", cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Receipt #{id} not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/post")]
        public async Task<ActionResult<ReceiptEntryDto>> Post(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            try
            {
                var result = await _receiptService.PostReceiptAsync(id, payload, "Finance User", cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Receipt #{id} not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/receive")]
        public async Task<ActionResult<ReceiptEntryDto>> RecordReceived(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            try
            {
                var result = await _receiptService.RecordReceivedAsync(id, payload, "Finance User", cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Receipt #{id} not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<ActionResult<ReceiptEntryDto>> Cancel(int id, [FromBody] StatusActionRequestDto payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            try
            {
                var result = await _receiptService.CancelReceiptAsync(id, payload, "Finance User", cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Receipt #{id} not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/duplicate")]
        public async Task<ActionResult<ReceiptEntryDto>> Duplicate(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            try
            {
                var cloned = await _receiptService.DuplicateReceiptAsync(id, "Finance User", cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = cloned.Id }, cloned);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Receipt #{id} not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
