using System;
using System.Threading.Tasks;
using CRM.DTO;
using CRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Controllers
{
    [Route("api/admin/lead-tracker")]
    [ApiController]
    public class LeadTrackerController : ControllerBase
    {
        private readonly ILeadTrackerService _trackerService;
        private readonly ILogger<LeadTrackerController> _logger;

        public LeadTrackerController(ILeadTrackerService trackerService, ILogger<LeadTrackerController> logger)
        {
            _trackerService = trackerService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves paged, filtered lead tracker matrix records and summary KPIs.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTrackerData([FromQuery] LeadTrackerQueryParams filters)
        {
            try
            {
                var result = await _trackerService.GetPagedTrackerDataAsync(filters);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LeadTrackerController] Error fetching Lead Tracker data with filters: {@Filters}", filters);
                var details = new
                {
                    Message = "Error fetching Lead Tracker data",
                    Error = ex.Message,
                    InnerError = ex.InnerException?.Message,
                    Source = ex.Source,
                    StackTrace = ex.StackTrace
                };
                return StatusCode(500, details);
            }
        }

        /// <summary>
        /// Downloads Excel report of filtered lead tracker matrix records.
        /// </summary>
        [HttpGet("export/excel")]
        public async Task<IActionResult> ExportToExcel([FromQuery] LeadTrackerQueryParams filters)
        {
            var fileBytes = await _trackerService.GenerateExcelExportAsync(filters);
            var fileName = $"Lead_Tracker_Matrix_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
