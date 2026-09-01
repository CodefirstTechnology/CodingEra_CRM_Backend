using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Dashboard;
using ERP.Application.Dashboard.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/admin-dashboard")]
    [ApiController]
    [Authorize]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IDashboardService _service;

        public AdminDashboardController(IDashboardService service)
        {
            _service = service;
        }

        [HttpGet("summary")]
        public async Task<ActionResult<AdminDashboardSummaryDto>> GetSummary(CancellationToken cancellationToken)
        {
            var summary = await _service.GetSummaryAsync(cancellationToken);
            return Ok(summary);
        }
    }
}
