using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;

namespace ERP.Application.Procurement
{
    public interface IVendorLedgerService
    {
        Task<VendorLedgerStatementResultDto> GetVendorLedgerStatementAsync(int vendorId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
        Task<List<VendorAgingSummaryDto>> GetVendorAgingSummaryAsync(CancellationToken cancellationToken = default);
    }
}
