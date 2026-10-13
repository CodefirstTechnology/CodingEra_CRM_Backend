using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CRM.DTO;
using CRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Controllers
{
    [Route("api/daily-sales-recognition")]
    [ApiController]
    public class DailySalesRecognitionController : ControllerBase
    {
        private readonly IDailySalesRecognitionService _service;

        public DailySalesRecognitionController(IDailySalesRecognitionService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardData(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? executive = null)
        {
            var data = await _service.GetDashboardDataAsync(startDate, endDate, executive);
            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLog([FromBody] DailySalesRecognitionUpsertDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.ExecutiveName))
            {
                return BadRequest("Executive name is required.");
            }

            var result = await _service.UpsertLogAsync(dto);
            return Ok(result);
        }

        [HttpPost("bulk")]
        public async Task<IActionResult> BulkUpsert([FromBody] List<DailySalesRecognitionUpsertDto> dtos)
        {
            if (dtos == null || dtos.Count == 0)
            {
                return BadRequest("Empty payload.");
            }

            var results = await _service.BulkUpsertLogsAsync(dtos);
            return Ok(results);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateLog(int id, [FromBody] DailySalesRecognitionUpsertDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.ExecutiveName))
            {
                return BadRequest("Executive name is required.");
            }

            dto.Id = id;
            var result = await _service.UpsertLogAsync(dto);
            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteLog(int id)
        {
            var success = await _service.DeleteLogAsync(id);
            if (!success) return NotFound();
            return Ok(new { message = "Log deleted successfully." });
        }

        [HttpGet("executives")]
        public async Task<IActionResult> GetExecutives()
        {
            var execs = await _service.GetActiveExecutivesAsync();
            return Ok(execs);
        }

        [HttpGet("autofill-metrics")]
        public async Task<IActionResult> AutofillMetrics(
            [FromQuery] DateTime date,
            [FromQuery] string executive)
        {
            if (string.IsNullOrWhiteSpace(executive))
            {
                return BadRequest("Executive name is required.");
            }

            var result = await _service.AutofillExecutiveMetricsAsync(date, executive);
            return Ok(result);
        }
    }
}
