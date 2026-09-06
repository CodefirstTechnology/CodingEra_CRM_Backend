using ERP.Shared.Models;
using ERP.Application.Common.Security;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [ApiController]
    [Route("api/quotations")]
    public class QuotationsController : ControllerBase
    {
        private readonly IQuotationService _quotationService;
        private readonly ISalesOrderService _salesOrderService;
        private readonly ICurrentUser _currentUser;

        public QuotationsController(
            IQuotationService quotationService,
            ISalesOrderService salesOrderService,
            ICurrentUser currentUser)
        {
            _quotationService = quotationService;
            _salesOrderService = salesOrderService;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<QuotationDto>>> GetAll(
            [FromQuery] QuotationListQueryDto query,
            CancellationToken cancellationToken)
        {
            var result = await _quotationService.GetPagedAsync(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("next-number")]
        public async Task<ActionResult<string>> GetNextNumber(CancellationToken cancellationToken)
        {
            var number = await _quotationService.GetNextNumberAsync(cancellationToken);
            return Ok(new { number });
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<QuotationDto>> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _quotationService.GetByIdAsync(id, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<QuotationDto>> Create(
            [FromBody] QuotationCreateRequestDto request,
            CancellationToken cancellationToken)
        {
            var user = ResolveActingUser();
            var result = await _quotationService.CreateAsync(request, user, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<QuotationDto>> Update(
            int id,
            [FromBody] QuotationUpdateRequestDto request,
            CancellationToken cancellationToken)
        {
            var user = ResolveActingUser();
            var result = await _quotationService.UpdateAsync(id, request, user, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken)
        {
            var user = ResolveActingUser();
            var deleted = await _quotationService.DeleteAsync(id, user, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }

        /// <summary>
        /// Authorizes commercial execution when the client accepts the quote (PO received or signed quote).
        /// Transitions status to 'Approved' (Client Accepted).
        /// </summary>
        [HttpPost("{id:int}/client-accept")]
        public async Task<ActionResult<QuotationDto>> MarkClientAccepted(
            int id,
            [FromBody] QuotationClientAcceptRequestDto request,
            CancellationToken cancellationToken)
        {
            var user = ResolveActingUser();
            var result = await _quotationService.MarkClientAcceptedAsync(id, request, user, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Versioning: Marks the existing quote as Revised (IsCurrentRevision = false) and clones a new Draft revision.
        /// </summary>
        [HttpPost("{id:int}/revise")]
        public async Task<ActionResult<QuotationDto>> Revise(
            int id,
            [FromBody] QuotationReviseRequestDto request,
            CancellationToken cancellationToken)
        {
            var user = ResolveActingUser();
            var result = await _quotationService.ReviseAsync(id, request, user, cancellationToken);
            return Ok(result);
        }

        [HttpPost("{id:int}/duplicate")]
        public async Task<ActionResult<QuotationDto>> Duplicate(
            int id,
            CancellationToken cancellationToken)
        {
            var user = ResolveActingUser();
            var result = await _quotationService.DuplicateAsync(id, user, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Converts an approved (client-accepted) quote to a Sales Order under row-level pessimistic lock.
        /// </summary>
        [HttpPost("{id:int}/convert-to-sales-order")]
        public async Task<ActionResult<SalesOrderDto>> ConvertToSalesOrder(
            int id,
            CancellationToken cancellationToken)
        {
            var user = ResolveActingUser();
            var so = await _salesOrderService.ConvertQuotationAsync(id, user, cancellationToken);
            return Ok(so);
        }

        private string ResolveActingUser()
        {
            if (_currentUser.IsAuthenticated)
            {
                if (!string.IsNullOrWhiteSpace(_currentUser.FullName))
                    return _currentUser.FullName;
                if (!string.IsNullOrWhiteSpace(_currentUser.Email))
                    return _currentUser.Email;
                if (_currentUser.UserId.HasValue)
                    return _currentUser.UserId.Value.ToString();
            }
            return "system";
        }
    }
}
