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
    [Route("api/gst")]
    public class GstController : ControllerBase
    {
        private readonly IGstService _gstService;

        public GstController(IGstService gstService)
        {
            _gstService = gstService;
        }

        [HttpGet("transactions")]
        public async Task<ActionResult<IReadOnlyList<GstTransactionListItemDto>>> GetTransactions(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? returnPeriod,
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
                ReturnPeriod = returnPeriod,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDir = sortDir
            };

            return Ok(await _gstService.GetGstTransactionsAsync(query, cancellationToken));
        }

        [HttpGet("transactions/{id:int}")]
        public async Task<ActionResult<GstTransactionDto>> GetTransactionById(int id, CancellationToken cancellationToken = default)
        {
            var item = await _gstService.GetGstTransactionByIdAsync(id, cancellationToken);
            return item is null ? NotFound(new { message = $"GST Transaction #{id} not found" }) : Ok(item);
        }

        [HttpPost("transactions")]
        public async Task<ActionResult<GstTransactionDto>> CreateTransaction([FromBody] GstTransactionDto dto, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            try
            {
                var created = await _gstService.CreateGstTransactionAsync(dto, "Tax Officer", cancellationToken);
                return CreatedAtAction(nameof(GetTransactionById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("transactions/{id:int}/verify")]
        public async Task<ActionResult<GstTransactionDto>> VerifyTransaction(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            try
            {
                var result = await _gstService.VerifyGstTransactionAsync(id, payload, "Tax Officer", cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"GST Transaction #{id} not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("returns")]
        public async Task<ActionResult<IReadOnlyList<GstReturnDto>>> GetReturns(
            [FromQuery] string? status,
            [FromQuery] string? returnPeriod,
            CancellationToken cancellationToken = default)
        {
            var query = new ListQueryDto
            {
                Status = status,
                ReturnPeriod = returnPeriod
            };

            return Ok(await _gstService.GetGstReturnsAsync(query, cancellationToken));
        }

        [HttpPost("returns/{id:int}/verify")]
        public async Task<ActionResult<GstReturnDto>> VerifyReturn(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            try
            {
                var result = await _gstService.VerifyGstReturnAsync(id, payload, "Tax Officer", cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"GST Return #{id} not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("returns/{id:int}/file")]
        public async Task<ActionResult<GstReturnDto>> FileReturn(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            try
            {
                var result = await _gstService.FileGstReturnAsync(id, payload, "Tax Officer", cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"GST Return #{id} not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("returns/{id:int}/close")]
        public async Task<ActionResult<GstReturnDto>> CloseReturn(int id, [FromBody] StatusActionRequestDto? payload, [FromQuery] int? userId, CancellationToken cancellationToken = default)
        {
            _ = userId;
            try
            {
                var result = await _gstService.CloseGstReturnAsync(id, payload, "Tax Officer", cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"GST Return #{id} not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("summary")]
        public async Task<ActionResult<IReadOnlyList<GstSummaryDto>>> GetSummary(
            [FromQuery] string? returnPeriod,
            CancellationToken cancellationToken = default)
        {
            var query = new ListQueryDto
            {
                ReturnPeriod = returnPeriod
            };

            return Ok(await _gstService.GetGstSummaryAsync(query, cancellationToken));
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<GstDashboardDto>> GetDashboard(CancellationToken cancellationToken = default)
        {
            return Ok(await _gstService.GetGstDashboardAsync(cancellationToken));
        }
    }
}
