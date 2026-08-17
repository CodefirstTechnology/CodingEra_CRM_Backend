using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/vendor-quotations")]
    [ApiController]
    public class VendorQuotationsController : ControllerBase
    {
        private readonly IVendorQuotationService _vqService;

        public VendorQuotationsController(IVendorQuotationService vqService)
        {
            _vqService = vqService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<VendorQuotationDto>>> GetAll(
            [FromQuery] int? rfqId,
            [FromQuery] int? vendorId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? userId = null,
            CancellationToken cancellationToken = default)
        {
            _ = userId;
            var result = await _vqService.GetAllAsync(rfqId, vendorId, page, pageSize, cancellationToken);
            return Ok(result);
        }

        [HttpGet("next-number")]
        public async Task<ActionResult<string>> GetNextNumber([FromQuery] int? userId, CancellationToken cancellationToken)
        {
            _ = userId;
            var number = await _vqService.GetNextQuotationNumberAsync(cancellationToken);
            return Ok(number);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<VendorQuotationDto>> GetById(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            _ = userId;
            var vq = await _vqService.GetByIdAsync(id, cancellationToken);
            if (vq is null)
            {
                return NotFound();
            }

            return Ok(vq);
        }

        [HttpPost]
        public async Task<ActionResult<VendorQuotationDto>> Create(
            [FromBody] VendorQuotationCreateRequestDto request,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var created = await _vqService.CreateAsync(
                request,
                ResolveActingUser(userId),
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = created.Id, userId }, created);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(
            int id,
            [FromQuery] int? userId,
            CancellationToken cancellationToken)
        {
            var success = await _vqService.DeleteAsync(
                id,
                ResolveActingUser(userId),
                cancellationToken);

            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        private static string ResolveActingUser(int? userId) =>
            userId is > 0 ? userId.Value.ToString() : "system";
    }
}
