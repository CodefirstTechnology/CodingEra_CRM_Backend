using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Accounting;
using ERP.Application.Accounting.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [ApiController]
    [Route("api/outstanding")]
    public class OutstandingController : ControllerBase
    {
        private readonly IOutstandingService _outstandingService;

        public OutstandingController(IOutstandingService outstandingService)
        {
            _outstandingService = outstandingService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<OutstandingRowDto>>> GetOutstanding(
            [FromQuery] string? search,
            [FromQuery] string? partyType,
            [FromQuery] string? status,
            [FromQuery] int? customerId,
            [FromQuery] int? vendorId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            var query = new ListQueryDto
            {
                Search = search,
                PartyType = partyType,
                Status = status,
                CustomerId = customerId,
                VendorId = vendorId,
                Page = page,
                PageSize = pageSize
            };

            return Ok(await _outstandingService.GetOutstandingAsync(query, cancellationToken));
        }

        [HttpGet("customers")]
        public async Task<ActionResult<IReadOnlyList<OutstandingRowDto>>> GetCustomerOutstanding(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int? customerId,
            CancellationToken cancellationToken = default)
        {
            var query = new ListQueryDto
            {
                Search = search,
                Status = status,
                CustomerId = customerId
            };

            return Ok(await _outstandingService.GetCustomerOutstandingAsync(query, cancellationToken));
        }

        [HttpGet("vendors")]
        public async Task<ActionResult<IReadOnlyList<OutstandingRowDto>>> GetVendorOutstanding(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int? vendorId,
            CancellationToken cancellationToken = default)
        {
            var query = new ListQueryDto
            {
                Search = search,
                Status = status,
                VendorId = vendorId
            };

            return Ok(await _outstandingService.GetVendorOutstandingAsync(query, cancellationToken));
        }

        [HttpGet("ageing")]
        public async Task<ActionResult<AgeingReportDto>> GetAgeingReport(
            [FromQuery] string? partyType,
            [FromQuery] string? search,
            [FromQuery] string? status,
            CancellationToken cancellationToken = default)
        {
            var query = new ListQueryDto
            {
                PartyType = partyType,
                Search = search,
                Status = status
            };

            return Ok(await _outstandingService.GetAgeingReportAsync(query, cancellationToken));
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<OutstandingDashboardDto>> GetDashboard(CancellationToken cancellationToken = default)
        {
            return Ok(await _outstandingService.GetOutstandingDashboardAsync(cancellationToken));
        }

        [HttpPost("settle")]
        public async Task<ActionResult<bool>> Settle(
            [FromQuery] string partyType,
            [FromQuery] string documentNumber,
            [FromQuery] decimal amount,
            CancellationToken cancellationToken = default)
        {
            var result = await _outstandingService.ApplySettlementAsync(partyType, documentNumber, amount, cancellationToken);
            return result ? Ok(true) : BadRequest(new { message = "Unable to apply settlement against outstanding record" });
        }
    }
}
