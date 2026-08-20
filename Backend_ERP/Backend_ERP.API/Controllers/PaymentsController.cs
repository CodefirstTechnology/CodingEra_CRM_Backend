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
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<PaymentListItemDto>>> GetPayments(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int? vendorId,
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
                VendorId = vendorId,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDir = sortDir
            };

            return Ok(await _paymentService.GetPaymentsAsync(query, cancellationToken));
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<PaymentDashboardDto>> GetDashboard(CancellationToken cancellationToken = default)
        {
            return Ok(await _paymentService.GetPaymentDashboardAsync(cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PaymentEntryDto>> GetById(int id, CancellationToken cancellationToken = default)
        {
            var item = await _paymentService.GetPaymentByIdAsync(id, cancellationToken);
            return item is null ? NotFound(new { message = $"Payment #{id} not found" }) : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<PaymentEntryDto>> Create([FromBody] PaymentCreateRequestDto dto, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            try
            {
                var created = await _paymentService.CreatePaymentAsync(dto, "Finance User", cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<PaymentEntryDto>> Update(int id, [FromBody] PaymentUpdateRequestDto dto, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            try
            {
                var updated = await _paymentService.UpdatePaymentAsync(id, dto, "Finance User", cancellationToken);
                return Ok(updated);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Payment #{id} not found" });
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
                var deleted = await _paymentService.DeletePaymentAsync(id, cancellationToken);
                return deleted ? NoContent() : NotFound(new { message = $"Payment #{id} not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/approve")]
        public async Task<ActionResult<PaymentEntryDto>> Approve(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            try
            {
                var result = await _paymentService.ApprovePaymentAsync(id, payload, "Finance User", cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Payment #{id} not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/post")]
        public async Task<ActionResult<PaymentEntryDto>> Post(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            try
            {
                var result = await _paymentService.PostPaymentAsync(id, payload, "Finance User", cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Payment #{id} not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/pay")]
        public async Task<ActionResult<PaymentEntryDto>> RecordPaid(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            try
            {
                var result = await _paymentService.RecordPaidAsync(id, payload, "Finance User", cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Payment #{id} not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<ActionResult<PaymentEntryDto>> Cancel(int id, [FromBody] StatusActionRequestDto payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            try
            {
                var result = await _paymentService.CancelPaymentAsync(id, payload, "Finance User", cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Payment #{id} not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/duplicate")]
        public async Task<ActionResult<PaymentEntryDto>> Duplicate(int id, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            try
            {
                var cloned = await _paymentService.DuplicatePaymentAsync(id, "Finance User", cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = cloned.Id }, cloned);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Payment #{id} not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
