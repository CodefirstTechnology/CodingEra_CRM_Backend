using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Backend_ERP.Infrastructure.Production;
using ERP.Application.Production.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [ApiController]
    [Route("api/production/dpr")]
    public class DailyProductionReportController : ControllerBase
    {
        private readonly IDailyProductionReportService _dprService;

        public DailyProductionReportController(IDailyProductionReportService dprService)
        {
            _dprService = dprService ?? throw new ArgumentNullException(nameof(dprService));
        }

        [HttpPost("generate")]
        public async Task<ActionResult<DailyProductionReportDto>> GenerateDpr(
            [FromBody] GenerateDprRequestDto request,
            CancellationToken ct = default)
        {
            try
            {
                var report = await _dprService.GenerateDailyProductionReportAsync(request, ct);
                return Ok(report);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPost("finalize")]
        public async Task<ActionResult<DailyProductionReportDto>> FinalizeDpr(
            [FromBody] FinalizeDprRequestDto request,
            CancellationToken ct = default)
        {
            try
            {
                var report = await _dprService.FinalizeReportAsync(request, ct);
                return Ok(report);
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

        [HttpGet]
        public async Task<ActionResult<List<DailyProductionReportDto>>> GetReportsByDate(
            [FromQuery] DateTime date,
            CancellationToken ct = default)
        {
            var reports = await _dprService.GetReportsByDateAsync(date, ct);
            return Ok(reports);
        }
    }
}
