using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Accounting;
using ERP.Application.Accounting.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [ApiController]
    [Route("api/customer-ledger")]
    public class CustomerLedgerController : ControllerBase
    {
        private readonly ICustomerLedgerService _ledgerService;

        public CustomerLedgerController(ICustomerLedgerService ledgerService)
        {
            _ledgerService = ledgerService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CustomerLedgerListItemDto>>> GetCustomerLedger(
            [FromQuery] string? search,
            [FromQuery] int? customerId,
            [FromQuery] string? status,
            [FromQuery] string? fromDate,
            [FromQuery] string? toDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortDir = null,
            CancellationToken cancellationToken = default)
        {
            var query = new ListQueryDto
            {
                Search = search,
                CustomerId = customerId,
                Status = status,
                FromDate = fromDate,
                ToDate = toDate,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDir = sortDir
            };

            var list = await _ledgerService.GetCustomerLedgerAsync(query, cancellationToken);
            return Ok(list);
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<CustomerLedgerDashboardDto>> GetDashboard(CancellationToken cancellationToken = default)
        {
            var dashboard = await _ledgerService.GetCustomerLedgerDashboardAsync(cancellationToken);
            return Ok(dashboard);
        }

        [HttpGet("statement/{customerId:int}")]
        public async Task<ActionResult<CustomerStatementResultDto>> GetCustomerStatement(int customerId, CancellationToken cancellationToken = default)
        {
            var statement = await _ledgerService.GetCustomerStatementAsync(customerId, cancellationToken);
            return statement is null ? NotFound(new { message = $"No statement found for customer #{customerId}" }) : Ok(statement);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CustomerLedgerEntryDto>> GetById(int id, CancellationToken cancellationToken = default)
        {
            var item = await _ledgerService.GetCustomerLedgerByIdAsync(id, cancellationToken);
            return item is null ? NotFound(new { message = $"Ledger entry #{id} not found" }) : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<CustomerLedgerEntryDto>> Create([FromBody] CustomerLedgerEntryDto dto, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            var created = await _ledgerService.CreateLedgerEntryAsync(dto, "Finance User", cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
    }
}
