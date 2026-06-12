using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly TaskDbcontext _context;
        private readonly IDashboardService _dashboard;
        private readonly IRbacService _rbac;

        public DashboardController(TaskDbcontext context, IDashboardService dashboard, IRbacService rbac)
        {
            _context = context;
            _dashboard = dashboard;
            _rbac = rbac;
        }

        [HttpGet("preferences")]
        public async Task<IActionResult> GetPreferences([FromQuery] int userId)
        {
            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null)
            {
                return auditErr;
            }

            return Ok(await _dashboard.GetPreferencesAsync(userId));
        }

        [HttpPut("preferences")]
        public async Task<IActionResult> UpdatePreferences(
            [FromQuery] int userId,
            [FromBody] UserDashboardPreferenceUpdateDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var adminErr = await AdminUserValidation.ValidateAdminUserAsync(_context, userId, _rbac);
            if (adminErr != null)
            {
                return adminErr;
            }

            return Ok(await _dashboard.UpdatePreferencesAsync(userId, dto));
        }

        [HttpGet("user-summary")]
        public async Task<IActionResult> GetUserSummary([FromQuery] int userId)
        {
            var adminErr = await AdminUserValidation.ValidateAdminUserAsync(_context, userId, _rbac);
            if (adminErr != null)
            {
                return adminErr;
            }

            try
            {
                return Ok(await _dashboard.BuildAdminBusinessSummaryAsync(userId));
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiForbiddenResult.Create(ex.Message);
            }
        }

        [HttpGet("morning-briefing")]
        public async Task<IActionResult> GetMorningBriefing([FromQuery] int userId)
        {
            var adminErr = await AdminUserValidation.ValidateAdminUserAsync(_context, userId, _rbac);
            if (adminErr != null)
            {
                return adminErr;
            }

            try
            {
                var cached = await _dashboard.GetCachedMorningBriefingAsync(userId);
                if (cached == null)
                {
                    return NotFound(new { message = "No cached briefing for today. Open the admin dashboard to generate one." });
                }

                return Ok(cached);
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiForbiddenResult.Create(ex.Message);
            }
        }

        [HttpPost("morning-briefing")]
        public async Task<IActionResult> PostMorningBriefing(
            [FromQuery] int userId,
            [FromBody] DailyBriefingMetricsDto metrics,
            [FromQuery] bool regenerate = false)
        {
            if (metrics == null)
            {
                return BadRequest();
            }

            var adminErr = await AdminUserValidation.ValidateAdminUserAsync(_context, userId, _rbac);
            if (adminErr != null)
            {
                return adminErr;
            }

            try
            {
                return Ok(await _dashboard.GenerateMorningBriefingAsync(userId, metrics, regenerate));
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiForbiddenResult.Create(ex.Message);
            }
        }

        [HttpPost("morning-briefing/reset-daily")]
        public async Task<IActionResult> ResetBriefingDaily([FromQuery] int userId)
        {
            var adminErr = await AdminUserValidation.ValidateAdminUserAsync(_context, userId, _rbac);
            if (adminErr != null)
            {
                return adminErr;
            }

            try
            {
                await _dashboard.ResetBriefingForTestingAsync(userId);
                return Ok(new { reset = true });
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiForbiddenResult.Create(ex.Message);
            }
        }

        [HttpPost("morning-briefing/mark-played")]
        public async Task<IActionResult> MarkBriefingPlayed([FromQuery] int userId)
        {
            var adminErr = await AdminUserValidation.ValidateAdminUserAsync(_context, userId, _rbac);
            if (adminErr != null)
            {
                return adminErr;
            }

            await _dashboard.MarkBriefingPlayedAsync(userId);
            return Ok(new { played = true });
        }
    }
}
