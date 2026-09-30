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
    [Route("api/v1/batches")]
    [Produces("application/json")]
    public class BatchTrackingController : ControllerBase
    {
        private readonly IBatchAllocationService _batchService;

        public BatchTrackingController(IBatchAllocationService batchService)
        {
            _batchService = batchService;
        }

        /// <summary>
        /// Gets automated FEFO batch picking suggestions based on shelf-life rules
        /// </summary>
        [HttpGet("fefo-suggestions")]
        [ProducesResponseType(typeof(List<FEFOAllocationResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetFEFOSuggestions([FromQuery] BatchAllocationRequestDto request, CancellationToken ct)
        {
            var results = await _batchService.AllocateFEFOAsync(request, ct);
            return Ok(results);
        }

        /// <summary>
        /// Places an active batch on quarantine hold (Global Recall)
        /// </summary>
        [HttpPost("{id:guid}/quarantine")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> QuarantineBatch(Guid id, [FromBody] BatchQuarantineRequestDto request, CancellationToken ct)
        {
            await _batchService.QuarantineBatchAsync(id, request, ct);
            return Ok(new { batchId = id, status = "Quarantined", message = "Batch successfully placed on quarantine hold." });
        }
    }
}
