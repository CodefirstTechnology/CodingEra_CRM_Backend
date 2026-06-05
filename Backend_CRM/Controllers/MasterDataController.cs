using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Controllers
{
    /// <summary>Generic admin CRUD for CRM master lookup tables.</summary>
    [Route("api/master-data/{entity}")]
    [ApiController]
    public class MasterDataController : ControllerBase
    {
        private readonly TaskDbcontext _context;
        private readonly IMasterDataAdminService _masterData;

        public MasterDataController(TaskDbcontext context, IMasterDataAdminService masterData)
        {
            _context = context;
            _masterData = masterData;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            string entity,
            [FromQuery] int userId,
            [FromQuery] bool activeOnly = false)
        {
            var adminErr = await AdminUserValidation.ValidateAdminUserAsync(_context, userId);
            if (adminErr != null)
            {
                return adminErr;
            }

            if (!_masterData.IsSupportedEntity(entity))
            {
                return NotFound($"Unsupported master entity '{entity}'.");
            }

            var rows = await _masterData.ListAsync(entity, activeOnly);
            return Ok(rows);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(string entity, int id, [FromQuery] int userId)
        {
            var adminErr = await AdminUserValidation.ValidateAdminUserAsync(_context, userId);
            if (adminErr != null)
            {
                return adminErr;
            }

            if (!_masterData.IsSupportedEntity(entity))
            {
                return NotFound($"Unsupported master entity '{entity}'.");
            }

            var row = await _masterData.GetByIdAsync(entity, id);
            if (row == null)
            {
                return NotFound();
            }

            return Ok(row);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            string entity,
            [FromQuery] int userId,
            [FromBody] MasterDataUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var adminErr = await AdminUserValidation.ValidateAdminUserAsync(_context, userId);
            if (adminErr != null)
            {
                return adminErr;
            }

            if (!_masterData.IsSupportedEntity(entity))
            {
                return NotFound($"Unsupported master entity '{entity}'.");
            }

            AuditUserValidation.SetAuditUser(_context, userId);

            try
            {
                var (row, error) = await _masterData.CreateAsync(entity, dto);
                if (error != null)
                {
                    return error.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                        ? Conflict(error)
                        : BadRequest(error);
                }

                return Ok(row);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            string entity,
            int id,
            [FromQuery] int userId,
            [FromBody] MasterDataUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var adminErr = await AdminUserValidation.ValidateAdminUserAsync(_context, userId);
            if (adminErr != null)
            {
                return adminErr;
            }

            if (!_masterData.IsSupportedEntity(entity))
            {
                return NotFound($"Unsupported master entity '{entity}'.");
            }

            AuditUserValidation.SetAuditUser(_context, userId);

            try
            {
                var (row, error, notFound) = await _masterData.UpdateAsync(entity, id, dto);
                if (notFound)
                {
                    return NotFound();
                }

                if (error != null)
                {
                    return error.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                        ? Conflict(error)
                        : BadRequest(error);
                }

                return Ok(row);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPut("reorder")]
        public async Task<IActionResult> Reorder(
            string entity,
            [FromQuery] int userId,
            [FromBody] DealStatusReorderDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            if (!string.Equals(entity, "deal-statuses", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound($"Reorder is not supported for '{entity}'.");
            }

            var adminErr = await AdminUserValidation.ValidateAdminUserAsync(_context, userId);
            if (adminErr != null)
            {
                return adminErr;
            }

            AuditUserValidation.SetAuditUser(_context, userId);

            var (rows, error) = await _masterData.ReorderDealStatusesAsync(dto);
            if (error != null)
            {
                return BadRequest(error);
            }

            return Ok(rows);
        }

        [HttpPatch("{id:int}/active")]
        public async Task<IActionResult> PatchActive(
            string entity,
            int id,
            [FromQuery] int userId,
            [FromBody] MasterDataActivePatchDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var adminErr = await AdminUserValidation.ValidateAdminUserAsync(_context, userId);
            if (adminErr != null)
            {
                return adminErr;
            }

            if (!_masterData.IsSupportedEntity(entity))
            {
                return NotFound($"Unsupported master entity '{entity}'.");
            }

            AuditUserValidation.SetAuditUser(_context, userId);

            try
            {
                var (row, notFound) = await _masterData.PatchActiveAsync(entity, id, dto.IsActive);
                if (notFound)
                {
                    return NotFound();
                }

                return Ok(row);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
