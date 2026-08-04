using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/sales-orders")]
    [ApiController]
    public class SalesOrdersController : ControllerBase
    {
        private readonly ISalesOrderService _salesOrders;

        public SalesOrdersController(ISalesOrderService salesOrders)
        {
            _salesOrders = salesOrders;
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
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
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
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/status")]
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
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/cancel")]
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
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id:int}/status-history")]
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
            userId is > 0 ? $"user:{userId}" : "system";
    }
}
