using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Accounting.Dtos;

namespace ERP.Application.Accounting
{
    public interface IPaymentService
    {
        Task<IReadOnlyList<PaymentListItemDto>> GetPaymentsAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default);
        Task<PaymentEntryDto?> GetPaymentByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<PaymentEntryDto> CreatePaymentAsync(PaymentCreateRequestDto dto, string user, CancellationToken cancellationToken = default);
        Task<PaymentEntryDto> UpdatePaymentAsync(int id, PaymentUpdateRequestDto dto, string user, CancellationToken cancellationToken = default);
        Task<bool> DeletePaymentAsync(int id, CancellationToken cancellationToken = default);
        Task<PaymentEntryDto> ApprovePaymentAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default);
        Task<PaymentEntryDto> PostPaymentAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default);
        Task<PaymentEntryDto> RecordPaidAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default);
        Task<PaymentEntryDto> CancelPaymentAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default);
        Task<PaymentEntryDto> DuplicatePaymentAsync(int id, string user, CancellationToken cancellationToken = default);
        Task<PaymentDashboardDto> GetPaymentDashboardAsync(CancellationToken cancellationToken = default);
    }
}
