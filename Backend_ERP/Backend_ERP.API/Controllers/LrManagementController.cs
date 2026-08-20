using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/dispatch-logistics/lrs")]
    [ApiController]
    public class LrManagementController : ControllerBase
    {
        private readonly ILrManagementService _service;

        public LrManagementController(ILrManagementService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<LrListItemDto>>> GetLrs(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int? dispatchId,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            CancellationToken cancellationToken)
        {
            var query = new ListQueryDto
            {
                Search = search,
                Status = status,
                DispatchId = dispatchId,
                DateFrom = dateFrom,
                DateTo = dateTo
            };

            var list = await _service.GetLrsAsync(query, cancellationToken);
            return Ok(list);
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<LrDashboardDto>> GetDashboard(CancellationToken cancellationToken)
        {
            var dashboard = await _service.GetLrDashboardAsync(cancellationToken);
            return Ok(dashboard);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<LrDto>> GetById(int id, CancellationToken cancellationToken)
        {
            try
            {
                var lr = await _service.GetLrByIdAsync(id, cancellationToken);
                return Ok(lr);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<LrDto>> Create(
            [FromBody] LrCreateRequestDto payload,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var created = await _service.CreateLrAsync(payload, currentUser, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<LrDto>> Update(
            int id,
            [FromBody] LrUpdateRequestDto payload,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var updated = await _service.UpdateLrAsync(id, payload, currentUser, cancellationToken);
                return Ok(updated);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                await _service.DeleteLrAsync(id, cancellationToken);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/generate")]
        public async Task<ActionResult<LrDto>> GenerateLrDocument(
            int id,
            [FromBody] StatusActionRequestDto? payload,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var result = await _service.GenerateLrDocumentAsync(id, payload, currentUser, cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/issue")]
        public async Task<ActionResult<LrDto>> IssueLr(
            int id,
            [FromBody] StatusActionRequestDto? payload,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var result = await _service.IssueLrAsync(id, payload, currentUser, cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/close")]
        public async Task<ActionResult<LrDto>> CloseLr(
            int id,
            [FromBody] StatusActionRequestDto? payload,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var result = await _service.CloseLrAsync(id, payload, currentUser, cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{lrId:int}/generate-eway")]
        public async Task<ActionResult<EwayDto>> GenerateEway(
            int lrId,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var result = await _service.GenerateEwayFromLrAsync(lrId, currentUser, cancellationToken);
                return Created($"/api/dispatch-logistics/eways/{result.Id}", result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private string GetCurrentUser()
        {
            return User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue(ClaimTypes.Email)
                ?? "System";
        }
    }
}
