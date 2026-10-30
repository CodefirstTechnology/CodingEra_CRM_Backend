using System.Threading;
using System.Threading.Tasks;

namespace ERP.Application.Sales
{
    public interface ISalesTargetRealizationEngine
    {
        Task RecordOrderBookingAsync(int salesOrderId, CancellationToken ct = default);

        Task ProcessOrderClawbackAsync(int salesOrderId, string reason, CancellationToken ct = default);

        Task RecordAdvanceCashAsync(int advancePaymentId, CancellationToken ct = default);

        Task ProcessAdvanceClawbackAsync(int advancePaymentId, string reason, CancellationToken ct = default);

        Task RecordInvoiceDispatchedAsync(int salesInvoiceId, CancellationToken ct = default);
    }
}

