using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement
{
    public interface IVendorPayablesLedgerEngine
    {
        Task<VendorLedgerEntry> PostBillPayableAsync(int purchaseBillId, int? userId = null, CancellationToken ct = default);
        Task<VendorLedgerEntry> PostPaymentDisbursementAsync(int vendorPaymentId, int? userId = null, CancellationToken ct = default);
        Task<VendorLedgerEntry> PostDebitNoteAsync(int debitNoteId, int vendorId, decimal amount, string referenceNumber, string narration, int? userId = null, CancellationToken ct = default);
        Task<VendorBillSettlementResultDto> SettleBillAsync(SettleBillRequestDto request, int? userId = null, CancellationToken ct = default);
        Task<VendorBillSettlementResultDto> AutoKnockOffFifoAsync(FifoKnockOffRequestDto request, int? userId = null, CancellationToken ct = default);
    }
}

