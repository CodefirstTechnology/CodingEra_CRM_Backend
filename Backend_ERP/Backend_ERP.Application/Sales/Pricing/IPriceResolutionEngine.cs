using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales.Pricing
{
    public interface IPriceResolutionEngine
    {
        Task<PriceListResolveDto> ResolveRateAsync(
            PriceListResolveRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
