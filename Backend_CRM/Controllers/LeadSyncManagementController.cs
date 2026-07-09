using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Controllers
{
    [Route("api/lead-sync-management")]
    [ApiController]
    public class LeadSyncManagementController : ControllerBase
    {
        private readonly TaskDbcontext _context;
        private readonly ILeadSyncManagementService _leadSync;
        private readonly IRbacService _rbac;

        public LeadSyncManagementController(
            TaskDbcontext context,
            ILeadSyncManagementService leadSync,
            IRbacService rbac)
        {
            _context = context;
            _leadSync = leadSync;
            _rbac = rbac;
        }

        [HttpGet("intervals")]
        public async Task<IActionResult> ListIntervals([FromQuery] int userId)
        {
            var err = await RequireAdminOrAssignedViewerAsync(userId);
            if (err != null) return err;
            return Ok(await _leadSync.ListIntervalsAsync());
        }

        [HttpGet("eligible-users")]
        public async Task<IActionResult> ListEligibleUsers([FromQuery] int userId)
        {
            var err = await RequireAdminAsync(userId);
            if (err != null) return err;
            return Ok(await _leadSync.ListEligibleUsersAsync());
        }

        [HttpGet("sources")]
        public async Task<IActionResult> ListSources([FromQuery] int userId)
        {
            var err = await RequireAdminAsync(userId);
            if (err != null) return err;
            return Ok(await _leadSync.ListSourcesForAdminAsync());
        }

        [HttpGet("my-access")]
        public async Task<IActionResult> ListMyAccess([FromQuery] int userId)
        {
            var permErr = await RbacAuthorization.RequirePermissionAsync(_context, _rbac, userId, "leads.view");
            if (permErr != null) return permErr;
            return Ok(await _leadSync.ListMyAccessAsync(userId));
        }

        [HttpPut("sources/{sourceId:int}/assignments")]
        public async Task<IActionResult> UpdateAssignments(
            int sourceId,
            [FromQuery] int userId,
            [FromBody] LeadSyncUpdateAssignmentsDto body)
        {
            var err = await RequireAdminAsync(userId);
            if (err != null) return err;

            try
            {
                await _leadSync.UpdateAssignmentsAsync(sourceId, body.UserIds, userId);
                return Ok(await _leadSync.ListSourcesForAdminAsync());
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("sources/{sourceId:int}/auto-sync")]
        public async Task<IActionResult> UpdateAutoSync(
            int sourceId,
            [FromQuery] int userId,
            [FromBody] LeadSyncUpdateAutoSyncDto body)
        {
            var err = await RequireAdminAsync(userId);
            if (err != null) return err;

            try
            {
                await _leadSync.UpdateAutoSyncAsync(sourceId, body, userId);
                return Ok(await _leadSync.ListSourcesForAdminAsync());
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("sources/{sourceId:int}/credentials")]
        public async Task<IActionResult> GetCredentials(int sourceId, [FromQuery] int userId)
        {
            var err = await RequireAdminAsync(userId);
            if (err != null) return err;
            return Ok(await _leadSync.GetCredentialsAsync(sourceId));
        }

        [HttpPut("sources/{sourceId:int}/credentials")]
        public async Task<IActionResult> SaveCredentials(
            int sourceId,
            [FromQuery] int userId,
            [FromBody] LeadSyncSaveCredentialsDto body)
        {
            var err = await RequireAdminAsync(userId);
            if (err != null) return err;

            try
            {
                var saved = await _leadSync.SaveCredentialsAsync(sourceId, body, userId);
                return Ok(await _leadSync.ListSourcesForAdminAsync());
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("sources/{sourceId:int}/test")]
        public async Task<IActionResult> TestConnection(int sourceId, [FromQuery] int userId)
        {
            var err = await RequireAdminAsync(userId);
            if (err != null) return err;
            return Ok(await _leadSync.TestConnectionAsync(sourceId));
        }

        [HttpPost("sources/{sourceId:int}/sync")]
        public async Task<IActionResult> RunSync(int sourceId, [FromQuery] int userId)
        {
            var permErr = await RbacAuthorization.RequirePermissionAsync(_context, _rbac, userId, "leads.view");
            if (permErr != null) return permErr;

            try
            {
                return Ok(await _leadSync.RunManualSyncAsync(sourceId, userId));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        [HttpPost("manual-log")]
        public async Task<IActionResult> RecordManualLog(
            [FromQuery] int userId,
            [FromBody] LeadSyncManualLogDto body)
        {
            var permErr = await RbacAuthorization.RequirePermissionAsync(_context, _rbac, userId, "leads.view");
            if (permErr != null) return permErr;

            try
            {
                await _leadSync.RecordManualSyncLogAsync(userId, body);
                return Ok(new { recorded = true });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> ListHistory(
            [FromQuery] int userId,
            [FromQuery] LeadSyncLogQueryDto query)
        {
            var err = await RequireAdminAsync(userId);
            if (err != null) return err;
            return Ok(await _leadSync.ListLogsAsync(query));
        }

        private async Task<IActionResult?> RequireAdminAsync(int userId)
        {
            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null) return auditErr;

            if (!await _rbac.IsAdminUserAsync(userId))
            {
                return Forbid("Only administrators can manage lead sync settings.");
            }

            return null;
        }

        private async Task<IActionResult?> RequireAdminOrAssignedViewerAsync(int userId)
        {
            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null) return auditErr;

            if (await _rbac.IsAdminUserAsync(userId))
            {
                return null;
            }

            var access = await _leadSync.ListMyAccessAsync(userId);
            if (access.Count == 0)
            {
                return Forbid("Lead sync access required.");
            }

            return null;
        }
    }
}
