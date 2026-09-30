using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Backend_ERP.Application.DTOs.Inventory;
using Backend_ERP.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend_ERP.API.Controllers
{
    [ApiController]
    [Route("api/v1/stock-verification")]
    [Produces("application/json")]
    public class PhysicalStockVerificationController : ControllerBase
    {
        private readonly IPhysicalStockVerificationService _verificationService;

        public PhysicalStockVerificationController(IPhysicalStockVerificationService verificationService)
        {
            _verificationService = verificationService;
        }

        /// <summary>
        /// Creates a new Physical Count Sheet for a warehouse count cycle
        /// </summary>
        [HttpPost("sheets")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateSheet([FromBody] CreateCountSheetDto request, CancellationToken ct)
        {
            var sheetNo = await _verificationService.CreateCountSheetAsync(request, ct);
            return CreatedAtAction(nameof(CreateSheet), new { sheetNo }, new { sheetNumber = sheetNo, status = "Draft" });
        }

        /// <summary>
        /// Retrieves details of a specific Physical Count Sheet
        /// </summary>
        [HttpGet("sheets/{id:guid}")]
        [ProducesResponseType(typeof(CountSheetDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSheet(Guid id, CancellationToken ct)
        {
            var sheet = await _verificationService.GetCountSheetAsync(id, ct);
            if (sheet == null) return NotFound(new { message = $"Count sheet {id} not found." });
            return Ok(sheet);
        }

        /// <summary>
        /// Submits updated physical counts for an existing count sheet
        /// </summary>
        [HttpPut("sheets/{id:guid}/counts")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SubmitCounts(Guid id, [FromBody] List<CountLineCreateDto> counts, CancellationToken ct)
        {
            await _verificationService.SubmitCountsAsync(id, counts, ct);
            return Ok(new { sheetId = id, status = "Submitted", message = "Physical counts submitted successfully." });
        }

        /// <summary>
        /// Reconciles stock variances by dispatching ledger gain/loss adjustments
        /// </summary>
        [HttpPost("sheets/{id:guid}/reconcile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReconcileSheet(Guid id, [FromBody] ReconcileCountSheetDto request, CancellationToken ct)
        {
            var sheetNo = await _verificationService.ReconcileCountSheetAsync(id, request, ct);
            return Ok(new { sheetNumber = sheetNo, status = "Reconciled", message = "Stock count sheet reconciled and inventory adjusted." });
        }
    }
}
