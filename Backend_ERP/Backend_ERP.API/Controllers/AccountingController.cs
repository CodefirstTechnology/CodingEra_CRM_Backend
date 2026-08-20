using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Accounting;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [ApiController]
    [Route("api/accounting")]
    public class AccountingController : ControllerBase
    {
        private readonly IAccountingFoundationService _foundationService;

        public AccountingController(IAccountingFoundationService foundationService)
        {
            _foundationService = foundationService;
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<IReadOnlyList<string>>> GetPermissions(CancellationToken cancellationToken = default)
        {
            return Ok(await _foundationService.GetPermissionsAsync(cancellationToken));
        }

        [HttpGet("health")]
        public ActionResult<object> GetHealth()
        {
            return Ok(new
            {
                Module = "Accounting",
                Status = "Healthy",
                Phase = 1
            });
        }
    }
}
