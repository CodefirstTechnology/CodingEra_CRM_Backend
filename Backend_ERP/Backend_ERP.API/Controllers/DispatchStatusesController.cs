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
    [Route("api/dispatch-logistics/statuses")]
    [ApiController]
    public class DispatchStatusesController : ControllerBase
    {
        private readonly IDispatchStatusService _service;

        public DispatchStatusesController(IDispatchStatusService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<DispatchStatusListItemDto>>> GetDispatchStatuses(
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

            var list = await _service.GetDispatchStatusesAsync(query, cancellationToken);
            return Ok(list);
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<DispatchStatusDashboardDto>> GetDashboard(CancellationToken cancellationToken)
        {
            var dashboard = await _service.GetDispatchStatusDashboardAsync(cancellationToken);
            return Ok(dashboard);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<DispatchStatusDto>> GetById(int id, CancellationToken cancellationToken)
        {
            try
            {
                var status = await _service.GetDispatchStatusByIdAsync(id, cancellationToken);
                return Ok(status);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<DispatchStatusDto>> Update(
            int id,
            [FromBody] DispatchStatusUpdateRequestDto payload,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = GetCurrentUser();
                var updated = await _service.UpdateDispatchStatusAsync(id, payload, currentUser, cancellationToken);
                return Ok(updated);
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

        private string GetCurrentUser()
        {
            return User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue(ClaimTypes.Email)
                ?? "System";
        }
    }
}
