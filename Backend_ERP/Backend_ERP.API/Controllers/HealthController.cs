using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/health")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { status = "ERP Backend Running" });
        }
    }
}
