using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Controllers
{
    [Route("api/company-profile")]
    [ApiController]
    public class CompanyProfileController : ControllerBase
    {
        private readonly TaskDbcontext _context;
        private readonly ICompanyProfileService _companyProfileService;
        private readonly IRbacService _rbac;

        public CompanyProfileController(
            TaskDbcontext context,
            ICompanyProfileService companyProfileService,
            IRbacService rbac)
        {
            _context = context;
            _companyProfileService = companyProfileService;
            _rbac = rbac;
        }

        [HttpGet("branding")]
        public async Task<IActionResult> GetBranding()
        {
            return Ok(await _companyProfileService.GetBrandingAsync());
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int userId)
        {
            var permErr = await RbacAuthorization.RequireAnyPermissionAsync(
                _context, _rbac, userId, "settings.view", "settings.manage", "quotations.view");
            if (permErr != null)
            {
                return permErr;
            }

            return Ok(await _companyProfileService.GetAsync());
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromQuery] int userId, [FromBody] CompanyProfileUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var permErr = await RbacAuthorization.RequirePermissionAsync(_context, _rbac, userId, "settings.manage");
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
                return Ok(await _companyProfileService.UpdateAsync(dto));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
