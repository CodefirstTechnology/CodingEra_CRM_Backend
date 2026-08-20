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
    [Route("api/dispatch-logistics/pods")]
    [ApiController]
    public class PodsController : ControllerBase
    {
        private readonly IPodService _service;

        public PodsController(IPodService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<PodListItemDto>>> GetPods(
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

            var list = await _service.GetPodsAsync(query, cancellationToken);
            return Ok(list);
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<PodDashboardDto>> GetDashboard(CancellationToken cancellationToken)
        {
            var dashboard = await _service.GetPodDashboardAsync(cancellationToken);
            return Ok(dashboard);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PodDto>> GetById(int id, CancellationToken cancellationToken)
        {
            try
            {
                var pod = await _service.GetPodByIdAsync(id, cancellationToken);
                return Ok(pod);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<PodDto>> Create(
            [FromBody] PodCreateRequestDto payload,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var created = await _service.CreatePodAsync(payload, currentUser, cancellationToken);
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
        public async Task<ActionResult<PodDto>> Update(
            int id,
            [FromBody] PodUpdateRequestDto payload,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var updated = await _service.UpdatePodAsync(id, payload, currentUser, cancellationToken);
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
                await _service.DeletePodAsync(id, cancellationToken);
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

        [HttpPost("{id:int}/delivered")]
        public async Task<ActionResult<PodDto>> MarkDelivered(
            int id,
            [FromBody] StatusActionRequestDto? payload,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var result = await _service.MarkPodDeliveredAsync(id, payload, currentUser, cancellationToken);
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

        [HttpPost("{id:int}/confirm")]
        public async Task<ActionResult<PodDto>> ConfirmDelivery(
            int id,
            [FromBody] StatusActionRequestDto? payload,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var result = await _service.ConfirmDeliveryAsync(id, payload, currentUser, cancellationToken);
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
        public async Task<ActionResult<PodDto>> Close(
            int id,
            [FromBody] StatusActionRequestDto? payload,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var result = await _service.ClosePodAsync(id, payload, currentUser, cancellationToken);
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

        private string GetCurrentUser()
        {
            return User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue(ClaimTypes.Email)
                ?? "System";
        }
    }
}
