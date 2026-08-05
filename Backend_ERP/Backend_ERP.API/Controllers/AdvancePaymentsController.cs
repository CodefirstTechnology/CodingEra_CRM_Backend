using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/advance-payments")]
    [ApiController]
    public class AdvancePaymentsController : ControllerBase
    {
        private readonly IAdvancePaymentService _service;

        public AdvancePaymentsController(IAdvancePaymentService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<AdvancePaymentListItemDto>>> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? customer,
            [FromQuery] string? paymentMode,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var rows = await _service.GetAllAsync(new AdvancePaymentListQueryDto
            {
                Search = search,
                Status = status,
                Customer = customer,
                PaymentMode = paymentMode,
                DateFrom = dateFrom,
                DateTo = dateTo
            }, cancellationToken);
            return Ok(rows);
        }

        [HttpGet("ledger")]
        public async Task<ActionResult<IReadOnlyList<AdvancePaymentLedgerDto>>> Ledger(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? customer,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.GetLedgerAsync(new AdvancePaymentListQueryDto
            {
                Search = search,
                Status = status,
                Customer = customer,
                DateFrom = dateFrom,
                DateTo = dateTo
            }, cancellationToken));
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<AdvancePaymentDashboardDto>> Dashboard(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.GetDashboardAsync(cancellationToken));
        }

        [HttpGet("reports")]
        public async Task<ActionResult<AdvancePaymentReportDto>> Reports(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.GetReportsAsync(new AdvancePaymentListQueryDto
            {
                Search = search,
                Status = status,
                DateFrom = dateFrom,
                DateTo = dateTo
            }, cancellationToken));
        }

        [HttpPost("reports/export")]
        public async Task<ActionResult<AdvancePaymentExportMetadataDto>> ExportReports(
            [FromBody] AdvancePaymentExportRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.ExportReportsAsync(request, cancellationToken));
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<IReadOnlyList<string>>> Permissions([FromQuery] int? userId)
        {
            _ = userId;
            return Ok(await _service.GetPermissionsAsync());
        }

        [HttpGet("lookups/customers")]
        public async Task<ActionResult<IReadOnlyList<AdvancePaymentLookupCustomerDto>>> LookupCustomers(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.LookupCustomersAsync(cancellationToken));
        }

        [HttpGet("lookups/sales-orders")]
        public async Task<ActionResult<IReadOnlyList<AdvancePaymentLookupSalesOrderDto>>> LookupSalesOrders(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.LookupSalesOrdersAsync(cancellationToken));
        }

        [HttpGet("lookups/quotations")]
        public async Task<ActionResult<IReadOnlyList<AdvancePaymentLookupQuotationDto>>> LookupQuotations(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.LookupQuotationsAsync(cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<AdvancePaymentDto>> GetById(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var row = await _service.GetByIdAsync(id, cancellationToken);
            return row is null ? NotFound() : Ok(row);
        }

        [HttpPost]
        public async Task<ActionResult<AdvancePaymentDto>> Create(
            [FromBody] AdvancePaymentCreateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var created = await _service.CreateAsync(request, ResolveActingUser(userId), cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.Id, userId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<AdvancePaymentDto>> Update(
            int id,
            [FromBody] AdvancePaymentUpdateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var updated = await _service.UpdateAsync(id, request, ResolveActingUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var ok = await _service.DeleteAsync(id, ResolveActingUser(userId), cancellationToken);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/submit")]
        public async Task<ActionResult<AdvancePaymentDto>> Submit(
            int id,
            [FromBody] AdvancePaymentRemarksRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            return await Workflow(id, request, userId, _service.SubmitAsync, cancellationToken);
        }

        [HttpPost("{id:int}/verify")]
        public async Task<ActionResult<AdvancePaymentDto>> Verify(
            int id,
            [FromBody] AdvancePaymentRemarksRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            return await Workflow(id, request, userId, _service.VerifyAsync, cancellationToken);
        }

        [HttpPost("{id:int}/receive")]
        public async Task<ActionResult<AdvancePaymentDto>> Receive(
            int id,
            [FromBody] AdvancePaymentRemarksRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            return await Workflow(id, request, userId, _service.ReceiveAsync, cancellationToken);
        }

        [HttpPost("{id:int}/reject")]
        public async Task<ActionResult<AdvancePaymentDto>> Reject(
            int id,
            [FromBody] AdvancePaymentRemarksRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            return await Workflow(id, request, userId, _service.RejectAsync, cancellationToken);
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<ActionResult<AdvancePaymentDto>> Cancel(
            int id,
            [FromBody] AdvancePaymentRemarksRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            return await Workflow(id, request, userId, _service.CancelAsync, cancellationToken);
        }

        [HttpPost("{id:int}/apply")]
        public async Task<ActionResult<AdvancePaymentDto>> Apply(
            int id,
            [FromBody] AdvancePaymentApplyRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var updated = await _service.ApplyAsync(id, request, ResolveActingUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpGet("{id:int}/timeline")]
        public async Task<ActionResult<IReadOnlyList<AdvancePaymentTimelineDto>>> Timeline(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var rows = await _service.GetTimelineAsync(id, cancellationToken);
            return rows is null ? NotFound() : Ok(rows);
        }

        private async Task<ActionResult<AdvancePaymentDto>> Workflow(
            int id,
            AdvancePaymentRemarksRequestDto? request,
            int? userId,
            Func<int, AdvancePaymentRemarksRequestDto?, string, CancellationToken, Task<AdvancePaymentDto?>> action,
            CancellationToken cancellationToken)
        {
            try
            {
                var updated = await action(id, request, ResolveActingUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        private static string ResolveActingUser(int? userId) =>
            userId is > 0 ? userId.Value.ToString() : "1";
    }
}
