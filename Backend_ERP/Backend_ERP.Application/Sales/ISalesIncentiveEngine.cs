using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;

namespace ERP.Application.Sales
{
    public interface ISalesIncentiveEngine
    {
        Task<SalesCommissionCalculationResult> CalculateCommissionForOrderAsync(
            int salesPersonUserId,
            int financialYear,
            decimal orderAmount,
            decimal grossMarginPercentage,
            string orderNumber,
            int orderId,
            decimal advancePaymentClearedPercentage,
            decimal splitPercentage = 100.0m,
            CancellationToken ct = default);

        Task<SalesCommissionLedger> AccrueOrderCommissionAsync(
            int salesPersonUserId,
            int financialYear,
            int orderId,
            string orderNumber,
            decimal orderAmount,
            decimal grossMarginPercentage,
            decimal advancePaymentClearedPercentage,
            decimal splitPercentage = 100.0m,
            CancellationToken ct = default);

        Task<SalesCommissionLedger?> RecordClawbackAsync(
            int salesPersonUserId,
            int financialYear,
            int orderId,
            string orderNumber,
            decimal originalCommissionAmount,
            string reason,
            CancellationToken ct = default);

        Task<int> ReleaseCashGatedCommissionsAsync(
            int orderId,
            decimal totalClearedAdvancePercentage,
            CancellationToken ct = default);

        Task<SalesIncentiveScheme?> GetActiveSchemeAsync(int financialYear, CancellationToken ct = default);
    }

    public record SalesCommissionCalculationResult(
        decimal BaseRateApplied,
        decimal AcceleratorMultiplier,
        decimal CalculatedCommission,
        bool MarginHurdlePassed,
        bool CashGatePassed,
        string Status,
        string Notes
    );
}

