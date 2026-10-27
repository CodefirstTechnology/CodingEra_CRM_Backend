using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface IProformaApprovalService
    {
        Task<bool> RequiresApprovalAsync(int proformaInvoiceId, CancellationToken ct = default);

        string GenerateHmacToken(int proformaInvoiceId, string userId, string decision, string tenantId = "default");

        bool VerifyHmacToken(int proformaInvoiceId, string userId, string decision, string token, string tenantId = "default");

        Task<ProformaInvoiceDto> ProcessDecisionAsync(
            int proformaInvoiceId,
            ProformaInvoiceApprovalRequestDto request,
            string actingUser,
            string? ipAddress = null,
            CancellationToken ct = default);
    }
}
