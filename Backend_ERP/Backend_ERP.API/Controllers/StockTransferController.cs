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
    [Route("api/v1/transfers")]
    [Produces("application/json")]
    public class StockTransferController : ControllerBase
    {
        private readonly IStockTransferService _transferService;

        public StockTransferController(IStockTransferService transferService)
        {
            _transferService = transferService;
        }

        /// <summary>
        /// Creates a new Intra or Inter-Warehouse Stock Transfer Order
        /// </summary>
        [HttpPost("orders")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateOrder([FromBody] TransferOrderCreateDto request, CancellationToken ct)
        {
            var transferNo = await _transferService.CreateTransferOrderAsync(request, ct);
            return CreatedAtAction(nameof(CreateOrder), new { transferNo }, new { transferNo, status = "Requested" });
        }

        /// <summary>
        /// Dispatches an approved stock transfer (moves stock to In-Transit)
        /// </summary>
        [HttpPost("{id:guid}/dispatch")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DispatchTransfer(Guid id, [FromBody] DispatchTransferRequestDto request, CancellationToken ct)
        {
            var transferNo = await _transferService.DispatchTransferAsync(id, request, ct);
            return Ok(new { transferNo, status = "Dispatched", message = "Stock transfer dispatched successfully." });
        }

        /// <summary>
        /// Receives and reconciles an incoming stock transfer
        /// </summary>
        [HttpPost("{id:guid}/receive")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReceiveTransfer(Guid id, [FromBody] ReceiveTransferRequestDto request, CancellationToken ct)
        {
            var transferNo = await _transferService.ReceiveTransferAsync(id, request, ct);
            return Ok(new { transferNo, status = "Received", message = "Stock transfer received and reconciled successfully." });
        }
    }
}
