using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Controllers
{
    [Route("api/user-targets")]
    [ApiController]
    public class UserTargetsController : ControllerBase
    {
        private readonly TaskDbcontext _context;
        private readonly IUserTargetService _userTargets;
        private readonly IRbacService _rbac;

        public UserTargetsController(
            TaskDbcontext context,
            IUserTargetService userTargets,
            IRbacService rbac)
        {
            _context = context;
            _userTargets = userTargets;
            _rbac = rbac;
        }

        [HttpGet("types")]
        public async Task<IActionResult> ListTypes([FromQuery] int userId)
        {
            var permErr = await RequireViewAsync(userId);
            if (permErr != null)
            {
                return permErr;
            }

            return Ok(await _userTargets.ListTargetTypesAsync());
        }

        [HttpGet("sales-users")]
        public async Task<IActionResult> ListSalesUsers([FromQuery] int userId)
        {
            var permErr = await RequireManageAsync(userId);
            if (permErr != null)
            {
                return permErr;
            }

            return Ok(await _userTargets.ListSalesUsersAsync());
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] int userId,
            [FromQuery] int? filterUserId = null,
            [FromQuery] bool includeInactive = false)
        {
            var permErr = await RequireViewAsync(userId);
            if (permErr != null)
            {
                return permErr;
            }

            var canManage = await CanManageAsync(userId);
            if (!canManage)
            {
                filterUserId = userId;
            }

            return Ok(await _userTargets.ListTargetsAsync(filterUserId, includeInactive));
        }

        [HttpGet("monitor")]
        public async Task<IActionResult> Monitor(
            [FromQuery] int userId,
            [FromQuery] UserTargetMonitorQueryDto query)
        {
            var permErr = await RequireManageAsync(userId);
            if (permErr != null)
            {
                return permErr;
            }

            return Ok(await _userTargets.ListMonitorAsync(query ?? new UserTargetMonitorQueryDto()));
        }

        [HttpGet("my-widgets")]
        public async Task<IActionResult> MyWidgets([FromQuery] int userId)
        {
            var permErr = await RequireViewAsync(userId);
            if (permErr != null)
            {
                return permErr;
            }

            return Ok(await _userTargets.ListMyWidgetsAsync(userId));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, [FromQuery] int userId)
        {
            var permErr = await RequireViewAsync(userId);
            if (permErr != null)
            {
                return permErr;
            }

            var row = await _userTargets.GetByIdAsync(id);
            if (row == null)
            {
                return NotFound();
            }

            var canManage = await CanManageAsync(userId);
            if (!canManage && row.UserId != userId)
            {
                return ApiForbiddenResult.Create("You can only view your own targets.");
            }

            return Ok(row);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] int userId, [FromBody] UserTargetUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var permErr = await RequireManageAsync(userId);
            if (permErr != null)
            {
                return permErr;
            }

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null)
            {
                return auditErr;
            }

            AuditUserValidation.SetAuditUser(_context, userId);

            try
            {
                return Ok(await _userTargets.CreateAsync(dto));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromQuery] int userId, [FromBody] UserTargetUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var permErr = await RequireManageAsync(userId);
            if (permErr != null)
            {
                return permErr;
            }

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null)
            {
                return auditErr;
            }

            AuditUserValidation.SetAuditUser(_context, userId);

            try
            {
                return Ok(await _userTargets.UpdateAsync(id, dto));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> SetStatus(
            int id,
            [FromQuery] int userId,
            [FromBody] UserTargetStatusPatchDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var permErr = await RequireManageAsync(userId);
            if (permErr != null)
            {
                return permErr;
            }

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null)
            {
                return auditErr;
            }

            AuditUserValidation.SetAuditUser(_context, userId);

            try
            {
                return Ok(await _userTargets.SetActiveAsync(id, dto.IsActive));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private async Task<IActionResult?> RequireViewAsync(int userId)
        {
            return await RbacAuthorization.RequireAnyPermissionAsync(
                _context,
                _rbac,
                userId,
                "user_targets.view",
                "user_targets.manage",
                "settings.manage");
        }

        private async Task<IActionResult?> RequireManageAsync(int userId)
        {
            return await RbacAuthorization.RequireAnyPermissionAsync(
                _context,
                _rbac,
                userId,
                "user_targets.manage",
                "settings.manage");
        }

        private async Task<bool> CanManageAsync(int userId)
        {
            var err = await RequireManageAsync(userId);
            return err == null;
        }
    }
}
