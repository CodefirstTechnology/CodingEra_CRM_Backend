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
    [Route("api/bank-reconciliations")]
    public class BankReconciliationsController : ControllerBase
    {
        private readonly IBankReconciliationService _bankReconService;

        public BankReconciliationsController(IBankReconciliationService bankReconService)
        {
            _bankReconService = bankReconService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<BankReconListItemDto>>> GetBankReconciliations(
            [FromQuery] string? search,
            [FromQuery] string? status,
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
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDir = sortDir
            };

            return Ok(await _bankReconService.GetBankReconciliationsAsync(query, cancellationToken));
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<BankReconDashboardDto>> GetDashboard(CancellationToken cancellationToken = default)
        {
            return Ok(await _bankReconService.GetBankReconciliationDashboardAsync(cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<BankReconciliationDto>> GetById(int id, CancellationToken cancellationToken = default)
        {
            var item = await _bankReconService.GetBankReconciliationByIdAsync(id, cancellationToken);
            return item is null ? NotFound(new { message = $"Bank reconciliation #{id} not found" }) : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<BankReconciliationDto>> Create([FromBody] BankReconCreateRequestDto dto, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            try
            {
                var created = await _bankReconService.CreateBankReconciliationAsync(dto, "Finance User", cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<BankReconciliationDto>> Update(int id, [FromBody] BankReconUpdateRequestDto dto, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            try
            {
                var updated = await _bankReconService.UpdateBankReconciliationAsync(id, dto, "Finance User", cancellationToken);
                return Ok(updated);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Bank reconciliation #{id} not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/verify")]
        public async Task<ActionResult<BankReconciliationDto>> Verify(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            try
            {
                var result = await _bankReconService.VerifyBankReconciliationAsync(id, payload, "Finance User", cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Bank reconciliation #{id} not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/reconcile")]
        public async Task<ActionResult<BankReconciliationDto>> Reconcile(int id, [FromBody] StatusActionRequestDto payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            try
            {
                var result = await _bankReconService.ReconcileBankAsync(id, payload, "Finance User", cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Bank reconciliation #{id} not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/close")]
        public async Task<ActionResult<BankReconciliationDto>> Close(int id, [FromBody] StatusActionRequestDto payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            try
            {
                var result = await _bankReconService.CloseBankReconciliationAsync(id, payload, "Finance User", cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Bank reconciliation #{id} not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
