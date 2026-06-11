using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Controllers
{
    [Route("api/item-master")]
    [ApiController]
    public class ItemMasterController : ControllerBase
    {
        private readonly TaskDbcontext _context;
        private readonly IItemMasterService _itemMaster;
        private readonly IRbacService _rbac;

        public ItemMasterController(TaskDbcontext context, IItemMasterService itemMaster, IRbacService rbac)
        {
            _context = context;
            _itemMaster = itemMaster;
            _rbac = rbac;
        }

        [HttpGet("groups")]
        public async Task<IActionResult> ListGroups([FromQuery] int userId, [FromQuery] bool activeOnly = false)
        {
            var permErr = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId, "items.view", "items.manage", "settings.manage");
            if (permErr != null) return permErr;

            return Ok(await _itemMaster.ListGroupsAsync(activeOnly));
        }

        [HttpGet("groups/{id:int}")]
        public async Task<IActionResult> GetGroup(int id, [FromQuery] int userId)
        {
            var permErr = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId, "items.view", "items.manage", "settings.manage");
            if (permErr != null) return permErr;

            var row = await _itemMaster.GetGroupAsync(id);
            return row == null ? NotFound() : Ok(row);
        }

        [HttpPost("groups")]
        public async Task<IActionResult> CreateGroup([FromQuery] int userId, [FromBody] ItemGroupUpsertDto dto)
        {
            if (dto == null) return BadRequest();

            var permErr = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId, "items.manage", "settings.manage");
            if (permErr != null) return permErr;

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null) return auditErr;

            AuditUserValidation.SetAuditUser(_context, userId);

            try
            {
                return Ok(await _itemMaster.CreateGroupAsync(dto));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("groups/{id:int}")]
        public async Task<IActionResult> UpdateGroup(int id, [FromQuery] int userId, [FromBody] ItemGroupUpsertDto dto)
        {
            if (dto == null) return BadRequest();

            var permErr = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId, "items.manage", "settings.manage");
            if (permErr != null) return permErr;

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null) return auditErr;

            AuditUserValidation.SetAuditUser(_context, userId);

            try
            {
                var row = await _itemMaster.UpdateGroupAsync(id, dto);
                return row == null ? NotFound() : Ok(row);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("groups/{id:int}")]
        public async Task<IActionResult> DeleteGroup(int id, [FromQuery] int userId)
        {
            var permErr = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId, "items.manage", "settings.manage");
            if (permErr != null) return permErr;

            try
            {
                var ok = await _itemMaster.DeleteGroupAsync(id);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("attributes")]
        public async Task<IActionResult> ListAttributes([FromQuery] int userId, [FromQuery] bool activeOnly = false)
        {
            var permErr = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId, "items.view", "items.manage", "settings.manage");
            if (permErr != null) return permErr;

            return Ok(await _itemMaster.ListAttributesAsync(activeOnly));
        }

        [HttpGet("attributes/{id:int}")]
        public async Task<IActionResult> GetAttribute(int id, [FromQuery] int userId)
        {
            var permErr = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId, "items.view", "items.manage", "settings.manage");
            if (permErr != null) return permErr;

            var row = await _itemMaster.GetAttributeAsync(id);
            return row == null ? NotFound() : Ok(row);
        }

        [HttpPost("attributes")]
        public async Task<IActionResult> CreateAttribute([FromQuery] int userId, [FromBody] ItemAttributeUpsertDto dto)
        {
            if (dto == null) return BadRequest();

            var permErr = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId, "items.manage", "settings.manage");
            if (permErr != null) return permErr;

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null) return auditErr;

            AuditUserValidation.SetAuditUser(_context, userId);

            try
            {
                return Ok(await _itemMaster.CreateAttributeAsync(dto));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("attributes/{id:int}")]
        public async Task<IActionResult> UpdateAttribute(int id, [FromQuery] int userId, [FromBody] ItemAttributeUpsertDto dto)
        {
            if (dto == null) return BadRequest();

            var permErr = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId, "items.manage", "settings.manage");
            if (permErr != null) return permErr;

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null) return auditErr;

            AuditUserValidation.SetAuditUser(_context, userId);

            try
            {
                var row = await _itemMaster.UpdateAttributeAsync(id, dto);
                return row == null ? NotFound() : Ok(row);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("attributes/{id:int}")]
        public async Task<IActionResult> DeleteAttribute(int id, [FromQuery] int userId)
        {
            var permErr = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId, "items.manage", "settings.manage");
            if (permErr != null) return permErr;

            try
            {
                var ok = await _itemMaster.DeleteAttributeAsync(id);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("quotation-catalog")]
        public async Task<IActionResult> GetQuotationCatalog([FromQuery] int userId)
        {
            var permErr = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId, "items.view", "items.manage", "settings.manage", "quotations.view", "quotations.manage");
            if (permErr != null) return permErr;

            return Ok(await _itemMaster.GetQuotationCatalogAsync());
        }

        [HttpGet("items")]
        public async Task<IActionResult> ListItems([FromQuery] int userId, [FromQuery] ItemListQueryDto query)
        {
            var permErr = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId, "items.view", "items.manage", "settings.manage");
            if (permErr != null) return permErr;

            query ??= new ItemListQueryDto();
            return Ok(await _itemMaster.ListItemsAsync(query));
        }

        [HttpGet("items/{id:int}")]
        public async Task<IActionResult> GetItem(int id, [FromQuery] int userId)
        {
            var permErr = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId, "items.view", "items.manage", "settings.manage");
            if (permErr != null) return permErr;

            var row = await _itemMaster.GetItemAsync(id);
            return row == null ? NotFound() : Ok(row);
        }

        [HttpPost("items")]
        public async Task<IActionResult> CreateItem([FromQuery] int userId, [FromBody] ItemUpsertDto dto)
        {
            if (dto == null) return BadRequest();

            var permErr = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId, "items.manage", "settings.manage");
            if (permErr != null) return permErr;

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null) return auditErr;

            AuditUserValidation.SetAuditUser(_context, userId);

            try
            {
                return Ok(await _itemMaster.CreateItemAsync(dto));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("items/{id:int}")]
        public async Task<IActionResult> UpdateItem(int id, [FromQuery] int userId, [FromBody] ItemUpsertDto dto)
        {
            if (dto == null) return BadRequest();

            var permErr = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId, "items.manage", "settings.manage");
            if (permErr != null) return permErr;

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null) return auditErr;

            AuditUserValidation.SetAuditUser(_context, userId);

            try
            {
                var row = await _itemMaster.UpdateItemAsync(id, dto);
                return row == null ? NotFound() : Ok(row);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("items/{id:int}")]
        public async Task<IActionResult> DeleteItem(int id, [FromQuery] int userId)
        {
            var permErr = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId, "items.manage", "settings.manage");
            if (permErr != null) return permErr;

            try
            {
                var ok = await _itemMaster.DeleteItemAsync(id);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("items/{id:int}/variants")]
        public async Task<IActionResult> CreateVariant(int id, [FromQuery] int userId, [FromBody] ItemVariantUpsertDto dto)
        {
            if (dto == null) return BadRequest();

            var permErr = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId, "items.manage", "settings.manage");
            if (permErr != null) return permErr;

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null) return auditErr;

            AuditUserValidation.SetAuditUser(_context, userId);

            try
            {
                var row = await _itemMaster.CreateVariantAsync(id, dto);
                return row == null ? NotFound() : Ok(row);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("items/{id:int}/variants/generate")]
        public async Task<IActionResult> GenerateVariants(int id, [FromQuery] int userId, [FromBody] ItemVariantGenerateDto dto)
        {
            if (dto == null) return BadRequest();

            var permErr = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId, "items.manage", "settings.manage");
            if (permErr != null) return permErr;

            var auditErr = await AuditUserValidation.ValidateAuditUserAsync(_context, userId);
            if (auditErr != null) return auditErr;

            AuditUserValidation.SetAuditUser(_context, userId);

            try
            {
                var row = await _itemMaster.GenerateVariantsAsync(id, dto);
                return row == null ? NotFound() : Ok(row);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("items/{parentId:int}/variants/{variantId:int}")]
        public async Task<IActionResult> DeleteVariant(int parentId, int variantId, [FromQuery] int userId)
        {
            var permErr = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId, "items.manage", "settings.manage");
            if (permErr != null) return permErr;

            var ok = await _itemMaster.DeleteVariantAsync(parentId, variantId);
            return ok ? NoContent() : NotFound();
        }
    }
}
