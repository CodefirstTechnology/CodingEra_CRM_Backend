using HRMS.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

[Route("api/health")]
[ApiController]
[RequirePermission(HrmsPermissions.HealthView)]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() =>
        Ok(new
        {
            status = "Healthy",
            service = "HRMS API",
            timestamp = DateTime.UtcNow
        });
}
