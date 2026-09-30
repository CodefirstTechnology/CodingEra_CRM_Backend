using System;
using System.Threading;
using System.Threading.Tasks;
using Backend_ERP.Application.DTOs.Inventory;
using Backend_ERP.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend_ERP.API.Controllers
{
    [ApiController]
    [Route("api/v1/valuation")]
    [Produces("application/json")]
    public class StockValuationController : ControllerBase
    {
        private readonly IStockValuationService _valuationService;

        public StockValuationController(IStockValuationService valuationService)
        {
            _valuationService = valuationService;
        }

        /// <summary>
        /// Retrieves real-time stock valuation summary (WAC and FIFO) for an item and warehouse
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(StockValuationSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetValuationSummary([FromQuery] int itemId, [FromQuery] Guid warehouseId, CancellationToken ct)
        {
            var summary = await _valuationService.GetValuationSummaryAsync(itemId, warehouseId, ct);
            if (summary == null) return NotFound(new { message = $"No valuation records found for item {itemId} at warehouse {warehouseId}." });
            return Ok(summary);
        }

        /// <summary>
        /// Records an inward inventory receipt and updates valuation layers & WAC
        /// </summary>
        [HttpPost("inward-receipt")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RecordInwardReceipt([FromBody] ValuationInwardReceiptDto request, CancellationToken ct)
        {
            await _valuationService.RecordInwardReceiptAsync(request, ct);
            return Ok(new { status = "Success", message = "Inward receipt recorded and valuation updated." });
        }

        /// <summary>
        /// Consumes stock valuation on outward stock movement (FIFO depletion)
        /// </summary>
        [HttpPost("outward-depletion")]
        [ProducesResponseType(typeof(ValuationDepletionResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DepleteOutwardStock([FromBody] ValuationOutwardDepletionDto request, CancellationToken ct)
        {
            var result = await _valuationService.ConsumeValuationOnStockOutAsync(request, ct);
            return Ok(result);
        }
    }
}
