using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;
using ERP.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.API.Controllers
{
    [ApiController]
    [Route("api/bank-accounts")]
    public class BankAccountsController : ControllerBase
    {
        private readonly ERPDbContext _db;

        public BankAccountsController(ERPDbContext db)
        {
            _db = db;
        }

        [HttpGet("active-lookup")]
        public async Task<ActionResult<IReadOnlyList<BankAccountLookupDto>>> GetActiveLookup(
            CancellationToken cancellationToken = default)
        {
            var list = await _db.BankAccounts
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.AccountName)
                .Select(x => new BankAccountLookupDto
                {
                    Id = x.Id,
                    AccountName = x.AccountName,
                    AccountNumber = x.AccountNumber,
                    BankName = x.BankName,
                    Branch = x.Branch,
                    IfscCode = x.IfscCode,
                    AccountType = x.AccountType,
                    Currency = x.Currency,
                    IsActive = x.IsActive
                })
                .ToListAsync(cancellationToken);

            return Ok(list);
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<BankAccountLookupDto>>> GetAll(
            CancellationToken cancellationToken = default)
        {
            return await GetActiveLookup(cancellationToken);
        }
    }
}
