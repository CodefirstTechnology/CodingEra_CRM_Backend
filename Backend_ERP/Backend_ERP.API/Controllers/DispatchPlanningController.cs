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
    [Route("api/dispatch-logistics/plans")]
    [ApiController]
    public class DispatchPlanningController : ControllerBase
    {
        private readonly IDispatchPlanningService _service;

        public DispatchPlanningController(IDispatchPlanningService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<DispatchPlanListItemDto>>> GetPlans(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int? customerId,
            [FromQuery] int? dispatchId,
            [FromQuery] string? dateFrom,
            [FromQuery] string? dateTo,
            CancellationToken cancellationToken)
        {
            var query = new ListQueryDto
            {
                Search = search,
                Status = status,
                CustomerId = customerId,
                DispatchId = dispatchId,
                DateFrom = dateFrom,
                DateTo = dateTo
            };

            var list = await _service.GetDispatchPlansAsync(query, cancellationToken);
            return Ok(list);
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<DispatchPlanDashboardDto>> GetDashboard(CancellationToken cancellationToken)
        {
            var dashboard = await _service.GetDispatchPlanDashboardAsync(cancellationToken);
            return Ok(dashboard);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<DispatchPlanDto>> GetById(int id, CancellationToken cancellationToken)
        {
            try
            {
                var plan = await _service.GetDispatchPlanByIdAsync(id, cancellationToken);
                return Ok(plan);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<DispatchPlanDto>> Create(
            [FromBody] DispatchPlanCreateRequestDto payload,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var created = await _service.CreateDispatchPlanAsync(payload, currentUser, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<DispatchPlanDto>> Update(
            int id,
            [FromBody] DispatchPlanUpdateRequestDto payload,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var updated = await _service.UpdateDispatchPlanAsync(id, payload, currentUser, cancellationToken);
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
                await _service.DeleteDispatchPlanAsync(id, cancellationToken);
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

        [HttpPost("{id:int}/duplicate")]
        public async Task<ActionResult<DispatchPlanDto>> Duplicate(int id, CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var duplicated = await _service.DuplicateDispatchPlanAsync(id, currentUser, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = duplicated.Id }, duplicated);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/plan")]
        public async Task<ActionResult<DispatchPlanDto>> Plan(
            int id,
            [FromBody] StatusActionRequestDto? payload,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var result = await _service.PlanDispatchAsync(id, payload, currentUser, cancellationToken);
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

        [HttpPost("{id:int}/approve")]
        public async Task<ActionResult<DispatchPlanDto>> Approve(
            int id,
            [FromBody] StatusActionRequestDto? payload,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var result = await _service.ApproveDispatchAsync(id, payload, currentUser, cancellationToken);
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

        [HttpPost("{id:int}/ready")]
        public async Task<ActionResult<DispatchPlanDto>> MarkReady(
            int id,
            [FromBody] StatusActionRequestDto? payload,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var result = await _service.MarkReadyForDispatchAsync(id, payload, currentUser, cancellationToken);
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
        public async Task<ActionResult<DispatchPlanDto>> Close(
            int id,
            [FromBody] StatusActionRequestDto? payload,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var result = await _service.CloseDispatchAsync(id, payload, currentUser, cancellationToken);
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
