using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/proforma-invoices")]
    [ApiController]
    public class ProformaInvoicesController : ControllerBase
    {
        private readonly IProformaInvoiceService _service;

        public ProformaInvoicesController(IProformaInvoiceService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProformaInvoiceListItemDto>>> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? customer,
            [FromQuery] string? salesPerson,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var rows = await _service.GetAllAsync(new ProformaInvoiceListQueryDto
            {
                Search = search,
                Status = status,
                Customer = customer,
                SalesPerson = salesPerson,
                DateFrom = dateFrom,
                DateTo = dateTo
            }, cancellationToken);
            return Ok(rows);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProformaInvoiceDto>> GetById(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var row = await _service.GetByIdAsync(id, cancellationToken);
            return row is null ? NotFound() : Ok(row);
        }

        [HttpPost]
        public async Task<ActionResult<ProformaInvoiceDto>> Create(
            [FromBody] ProformaInvoiceCreateRequestDto request,
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
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ProformaInvoiceDto>> Update(
            int id,
            [FromBody] ProformaInvoiceUpdateRequestDto request,
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
                return BadRequest(new { message = ex.Message });
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
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/duplicate")]
        public async Task<ActionResult<ProformaInvoiceDto>> Duplicate(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var created = await _service.DuplicateAsync(id, ResolveActingUser(userId), cancellationToken);
                return created is null ? NotFound() : Ok(created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/status")]
        public async Task<ActionResult<ProformaInvoiceDto>> UpdateStatus(
            int id,
            [FromBody] ProformaInvoiceStatusUpdateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var updated = await _service.UpdateStatusAsync(id, request, ResolveActingUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/approval")]
        public async Task<ActionResult<ProformaInvoiceDto>> Approval(
            int id,
            [FromBody] ProformaInvoiceApprovalRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var updated = await _service.ApplyApprovalAsync(id, request, ResolveActingUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/convert")]
        public async Task<ActionResult<ProformaInvoiceDto>> Convert(
            int id,
            [FromBody] ProformaInvoiceConvertRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var updated = await _service.ConvertAsync(
                    id,
                    request ?? new ProformaInvoiceConvertRequestDto(),
                    ResolveActingUser(userId),
                    cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id:int}/status-history")]
        public async Task<ActionResult<IReadOnlyList<ProformaInvoiceStatusHistoryDto>>> StatusHistory(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var rows = await _service.GetStatusHistoryAsync(id, cancellationToken);
            return rows is null ? NotFound() : Ok(rows);
        }

        [HttpGet("{id:int}/approval-history")]
        public async Task<ActionResult<IReadOnlyList<ProformaInvoiceApprovalHistoryDto>>> ApprovalHistory(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var rows = await _service.GetApprovalHistoryAsync(id, cancellationToken);
            return rows is null ? NotFound() : Ok(rows);
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<ProformaDashboardDto>> Dashboard(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.GetDashboardAsync(cancellationToken));
        }

        [HttpGet("reports")]
        public async Task<ActionResult<ProformaReportResultDto>> Reports(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.GetReportsAsync(new ProformaInvoiceListQueryDto
            {
                Search = search,
                Status = status,
                DateFrom = dateFrom,
                DateTo = dateTo
            }, cancellationToken));
        }

        [HttpPost("reports/export")]
        public async Task<ActionResult<ProformaExportMetadataDto>> ExportReports(
            [FromBody] ProformaExportRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.ExportReportsAsync(request, cancellationToken));
        }

        [HttpPost("{id:int}/pdf")]
        public async Task<ActionResult<ProformaPdfResultDto>> Pdf(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var result = await _service.GeneratePdfAsync(id, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost("{id:int}/email")]
        public async Task<ActionResult<ProformaEmailResultDto>> Email(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var result = await _service.SendEmailAsync(id, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<IReadOnlyList<string>>> Permissions([FromQuery] int? userId)
        {
            _ = userId;
            return Ok(await _service.GetPermissionsAsync());
        }

        [HttpGet("lookups/customers")]
        public async Task<ActionResult<IReadOnlyList<ProformaLookupCustomerDto>>> LookupCustomers(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.LookupCustomersAsync(cancellationToken));
        }

        [HttpGet("lookups/sales-orders")]
        public async Task<ActionResult<IReadOnlyList<ProformaLookupSalesOrderDto>>> LookupSalesOrders(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.LookupSalesOrdersAsync(cancellationToken));
        }

        [HttpGet("lookups/quotations")]
        public async Task<ActionResult<IReadOnlyList<ProformaLookupQuotationDto>>> LookupQuotations(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.LookupQuotationsAsync(cancellationToken));
        }

        private static string ResolveActingUser(int? userId) =>
            userId is > 0 ? userId.Value.ToString() : "1";
    }
}
