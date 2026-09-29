using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;

namespace ERP.Application.Procurement
{
    public interface IVendorPaymentService
    {
        Task<VendorPaymentDto> RecordPaymentAsync(CreateVendorPaymentDto dto, string currentUser, CancellationToken cancellationToken = default);
        Task<List<VendorPaymentDto>> GetVendorPaymentsByVendorAsync(int vendorId, CancellationToken cancellationToken = default);
    }
}
