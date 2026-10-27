using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface ICustomerCreditAssessmentService
    {
        Task<CreditAssessmentResultDto> AssessCustomerExposureAsync(
            int customerId,
            decimal currentOrderTotal,
            CancellationToken ct = default);
    }
}
