using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/vendor-ledger")]
    [ApiController]
    public class VendorLedgerController : ControllerBase
    {
        private readonly IVendorLedgerService _ledgerService;
        private readonly IVendorPaymentService _paymentService;

        public VendorLedgerController(IVendorLedgerService ledgerService, IVendorPaymentService paymentService)
        {
            _ledgerService = ledgerService;
            _paymentService = paymentService;
        }

        [HttpGet("statement/{vendorId:int}")]
        public async Task<ActionResult<VendorLedgerStatementResultDto>> GetLedgerStatement(
            int vendorId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            CancellationToken cancellationToken = default)
        {
            var start = fromDate ?? DateTime.UtcNow.AddMonths(-6);
            var end = toDate ?? DateTime.UtcNow;

            var result = await _ledgerService.GetVendorLedgerStatementAsync(vendorId, start, end, cancellationToken);
            return Ok(result);
        }

        [HttpGet("aging-summary")]
        public async Task<ActionResult<List<VendorAgingSummaryDto>>> GetAgingSummary(CancellationToken cancellationToken = default)
        {
            var result = await _ledgerService.GetVendorAgingSummaryAsync(cancellationToken);
            return Ok(result);
        }

        [HttpPost("payments")]
        public async Task<ActionResult<VendorPaymentDto>> RecordPayment(
            [FromBody] CreateVendorPaymentDto dto,
            [FromQuery] int? userId,
            CancellationToken cancellationToken = default)
        {
            var user = userId is > 0 ? userId.Value.ToString() : "system";
            var result = await _paymentService.RecordPaymentAsync(dto, user, cancellationToken);
            return Ok(result);
        }

        [HttpGet("payments/{vendorId:int}")]
        public async Task<ActionResult<List<VendorPaymentDto>>> GetPaymentsByVendor(
            int vendorId,
            CancellationToken cancellationToken = default)
        {
            var result = await _paymentService.GetVendorPaymentsByVendorAsync(vendorId, cancellationToken);
            return Ok(result);
        }
    }
}
