using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/price-lists")]
    [ApiController]
    public class PriceListsController : ControllerBase
    {
        private readonly IPriceListService _service;

        public PriceListsController(IPriceListService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<PriceListListItemDto>>> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? customerCategory,
            [FromQuery] string? currency,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.GetAllAsync(new PriceListListQueryDto
            {
                Search = search,
                Status = status,
                CustomerCategory = customerCategory,
                Currency = currency,
                DateFrom = dateFrom,
                DateTo = dateTo
            }, cancellationToken));
        }

        [HttpGet("active")]
        public async Task<ActionResult<IReadOnlyList<PriceListListItemDto>>> GetActive(
            [FromQuery] string? asOfDate,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.GetActiveAsync(asOfDate, cancellationToken));
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<PriceListDashboardDto>> Dashboard(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.GetDashboardAsync(cancellationToken));
        }

        [HttpGet("reports")]
        public async Task<ActionResult<PriceListReportDto>> Reports(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? customerCategory,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.GetReportsAsync(new PriceListListQueryDto
            {
                Search = search,
                Status = status,
                CustomerCategory = customerCategory
            }, cancellationToken));
        }

        [HttpPost("reports/export")]
        public async Task<ActionResult<PriceListExportMetadataDto>> ExportReports(
            [FromBody] PriceListExportRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.ExportReportsAsync(request, cancellationToken));
        }

        [HttpGet("compare")]
        public async Task<ActionResult<IReadOnlyList<PriceListCompareDto>>> Compare(
            [FromQuery] string? itemCode,
            [FromQuery] string? customerCategory,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.CompareAsync(itemCode, customerCategory, cancellationToken));
        }

        [HttpPost("resolve")]
        public async Task<ActionResult<PriceListResolveDto>> Resolve(
            [FromBody] PriceListResolveRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            try
            {
                var result = await _service.ResolveAsync(request, cancellationToken);
                return result is null ? NotFound() : Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpGet("history/{id:int}")]
        public async Task<ActionResult<IReadOnlyList<PriceListHistoryDto>>> History(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var rows = await _service.GetHistoryAsync(id, cancellationToken);
            return rows is null ? NotFound() : Ok(rows);
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<IReadOnlyList<string>>> Permissions([FromQuery] int? userId)
        {
            _ = userId;
            return Ok(await _service.GetPermissionsAsync());
        }

        [HttpGet("lookups/items")]
        public async Task<ActionResult<IReadOnlyList<PriceListLookupDto>>> LookupItems(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.LookupItemsAsync(cancellationToken));
        }

        [HttpGet("lookups/customer-categories")]
        public async Task<ActionResult<IReadOnlyList<PriceListLookupDto>>> LookupCustomerCategories(
            [FromQuery] int? userId)
        {
            _ = userId;
            return Ok(await _service.LookupCustomerCategoriesAsync());
        }

        [HttpGet("lookups/currencies")]
        public async Task<ActionResult<IReadOnlyList<PriceListLookupDto>>> LookupCurrencies(
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            return Ok(await _service.LookupCurrenciesAsync(cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PriceListDto>> GetById(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var row = await _service.GetByIdAsync(id, cancellationToken);
            return row is null ? NotFound() : Ok(row);
        }

        [HttpPost]
        public async Task<ActionResult<PriceListDto>> Create(
            [FromBody] PriceListCreateRequestDto request,
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
        public async Task<ActionResult<PriceListDto>> Update(
            int id,
            [FromBody] PriceListUpdateRequestDto request,
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

        [HttpPost("{id:int}/activate")]
        public async Task<ActionResult<PriceListDto>> Activate(
            int id,
            [FromBody] PriceListRemarksRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var updated = await _service.ActivateAsync(id, request, ResolveActingUser(userId), cancellationToken);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return ValidationProblem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{id:int}/clone")]
        public async Task<ActionResult<PriceListDto>> Clone(
            int id,
            [FromBody] PriceListRemarksRequestDto? request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            try
            {
                var created = await _service.CloneAsync(id, request, ResolveActingUser(userId), cancellationToken);
                return created is null ? NotFound() : Ok(created);
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
