using ERP.API.Security;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Shared.Security;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/sales-orders")]
    [ApiController]
    [RequirePermission(ErpPermissions.SalesOrders.View)]
    public class SalesOrdersController : ControllerBase
    {
        private readonly ISalesOrderService _salesOrders;
        private readonly IProformaInvoiceService _proformaInvoices;
        private readonly IAdvancePaymentService _advancePayments;

        public SalesOrdersController(
            ISalesOrderService salesOrders,
            IProformaInvoiceService proformaInvoices,
            IAdvancePaymentService advancePayments)
        {
            _salesOrders = salesOrders;
            _proformaInvoices = proformaInvoices;
            _advancePayments = advancePayments;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<SalesOrderListItemDto>>> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var rows = await _salesOrders.GetAllAsync(
                new SalesOrderListQueryDto
                {
                    Search = search,
                    Status = status,
                    DateFrom = dateFrom,
                    DateTo = dateTo
                },
                cancellationToken);
            return Ok(rows);
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<IReadOnlyList<string>>> Permissions([FromQuery] int? userId)
        {
            _ = userId;
            return Ok(await _salesOrders.GetPermissionsAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<SalesOrderDto>> GetById(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var row = await _salesOrders.GetByIdAsync(id, cancellationToken);
            if (row is null)
            {
                return NotFound();
            }

            return Ok(row);
        }

        [HttpPost]
        [RequirePermission(ErpPermissions.SalesOrders.Create)]
        public async Task<ActionResult<SalesOrderDto>> Create(
            [FromBody] SalesOrderCreateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var created = await _salesOrders.CreateAsync(
                    request,
                    ResolveActingUser(userId),
                    cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.Id, userId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPut("{id:int}")]
        [RequirePermission(ErpPermissions.SalesOrders.Edit)]
        public async Task<ActionResult<SalesOrderDto>> Update(
            int id,
            [FromBody] SalesOrderUpdateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var updated = await _salesOrders.UpdateAsync(
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
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/status")]
        [RequirePermission(ErpPermissions.SalesOrders.Confirm)]
        public async Task<ActionResult<SalesOrderDto>> UpdateStatus(
            int id,
            [FromBody] SalesOrderStatusUpdateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var updated = await _salesOrders.UpdateStatusAsync(
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
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/cancel")]
        [RequirePermission(ErpPermissions.SalesOrders.Cancel)]
        public async Task<ActionResult<SalesOrderDto>> Cancel(
            int id,
            [FromBody] SalesOrderCancelRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var updated = await _salesOrders.CancelAsync(
                    id,
                    request ?? new SalesOrderCancelRequestDto(),
                    ResolveActingUser(userId),
                    cancellationToken);
                if (updated is null)
                {
                    return NotFound();
                }

                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("convert-quotation/{quotationApprovalId:int}")]
        [RequirePermission(ErpPermissions.SalesOrders.Create)]
        public async Task<ActionResult<SalesOrderDto>> ConvertQuotation(
            int quotationApprovalId,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var created = await _salesOrders.ConvertQuotationAsync(
                    quotationApprovalId,
                    ResolveActingUser(userId),
                    cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.Id, userId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/proforma-invoice")]
        [RequirePermission(ErpPermissions.ProformaInvoices.Create)]
        public async Task<ActionResult<ProformaInvoiceDto>> GenerateProformaInvoice(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var created = await _proformaInvoices.GenerateFromSalesOrderAsync(
                    id,
                    ResolveActingUser(userId),
                    cancellationToken);
                return Ok(created);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/advance-payments/apply")]
        [RequirePermission(ErpPermissions.AdvancePayments.Apply)]
        public async Task<ActionResult<AdvancePaymentDto>> ApplyAdvancePayment(
            int id,
            [FromBody] AdvancePaymentApplyRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                request.SalesOrderId = id;
                var updated = await _advancePayments.ApplyAsync(
                    request.AdvancePaymentId > 0 ? request.AdvancePaymentId : id,
                    request,
                    ResolveActingUser(userId),
                    cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/pdf")]
        [RequirePermission(ErpPermissions.SalesOrders.GeneratePdf)]
        public async Task<ActionResult<SalesOrderPdfResultDto>> GeneratePdf(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var result = await _salesOrders.GeneratePdfAsync(id, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost("{id:int}/email")]
        [RequirePermission(ErpPermissions.SalesOrders.SendEmail)]
        public async Task<ActionResult<SalesOrderEmailResultDto>> SendEmail(
            int id,
            [FromBody] SalesOrderEmailRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _salesOrders.SendEmailAsync(
                    id,
                    request,
                    ResolveActingUser(userId),
                    cancellationToken);
                return result is null ? NotFound() : Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpGet("{id:int}/status-history")]
        [RequirePermission(ErpPermissions.SalesOrders.AuditView)]
        public async Task<ActionResult<IReadOnlyList<SalesOrderStatusHistoryDto>>> GetStatusHistory(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var history = await _salesOrders.GetStatusHistoryAsync(id, cancellationToken);
            if (history is null)
            {
                return NotFound();
            }

            return Ok(history);
        }

        private static string ResolveActingUser(int? userId) =>
            userId is > 0 ? userId.Value.ToString() : "system";
    }
}
