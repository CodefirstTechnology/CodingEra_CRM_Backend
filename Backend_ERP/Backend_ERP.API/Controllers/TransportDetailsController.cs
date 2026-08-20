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
    [Route("api/dispatch-logistics/transports")]
    [ApiController]
    public class TransportDetailsController : ControllerBase
    {
        private readonly ITransportDetailsService _service;

        public TransportDetailsController(ITransportDetailsService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<TransportListItemDto>>> GetTransports(
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

            var list = await _service.GetTransportsAsync(query, cancellationToken);
            return Ok(list);
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<TransportDashboardDto>> GetDashboard(CancellationToken cancellationToken)
        {
            var dashboard = await _service.GetTransportDashboardAsync(cancellationToken);
            return Ok(dashboard);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TransportDto>> GetById(int id, CancellationToken cancellationToken)
        {
            try
            {
                var transport = await _service.GetTransportByIdAsync(id, cancellationToken);
                return Ok(transport);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<TransportDto>> Create(
            [FromBody] TransportCreateRequestDto payload,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var created = await _service.CreateTransportAsync(payload, currentUser, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<TransportDto>> Update(
            int id,
            [FromBody] TransportUpdateRequestDto payload,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var updated = await _service.UpdateTransportAsync(id, payload, currentUser, cancellationToken);
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
                await _service.DeleteTransportAsync(id, cancellationToken);
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

        [HttpPost("{id:int}/start")]
        public async Task<ActionResult<TransportDto>> StartJourney(
            int id,
            [FromBody] StatusActionRequestDto? payload,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var result = await _service.StartJourneyAsync(id, payload, currentUser, cancellationToken);
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

        [HttpPost("{id:int}/in-transit")]
        public async Task<ActionResult<TransportDto>> MarkInTransit(
            int id,
            [FromBody] StatusActionRequestDto? payload,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var result = await _service.MarkInTransitAsync(id, payload, currentUser, cancellationToken);
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

        [HttpPost("{id:int}/delivered")]
        public async Task<ActionResult<TransportDto>> MarkDelivered(
            int id,
            [FromBody] StatusActionRequestDto? payload,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var result = await _service.MarkDeliveredAsync(id, payload, currentUser, cancellationToken);
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
        public async Task<ActionResult<TransportDto>> Close(
            int id,
            [FromBody] StatusActionRequestDto? payload,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var result = await _service.CloseTransportAsync(id, payload, currentUser, cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
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

        [HttpPost("{id:int}/generate-lr")]
        public async Task<ActionResult<LrDto>> GenerateLr(
            int id,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var result = await _service.GenerateLrAsync(id, currentUser, cancellationToken);
                return Created($"/api/dispatch-logistics/lrs/{result.Id}", result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
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
