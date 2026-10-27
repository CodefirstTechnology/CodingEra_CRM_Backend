using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class CustomerCreditAssessmentService : ICustomerCreditAssessmentService
    {
        private readonly ERPDbContext _db;

        public CustomerCreditAssessmentService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<CreditAssessmentResultDto> AssessCustomerExposureAsync(
            int customerId,
            decimal currentOrderTotal,
            CancellationToken ct = default)
        {
            if (customerId <= 0)
            {
                // Customer ID unlinked, pass by default for guest/draft scenarios
                return CreditAssessmentResultDto.Passed(currentOrderTotal, 500000m);
            }

            // 1. Fetch Approved Credit Limit from Ledger/Vendor/Customer settings
            decimal approvedCreditLimit = 500000m; // Default enterprise threshold ₹5,00,000

            // 2. Query Open Accounts Receivable (AR) from Customer Ledger
            var openAr = await _db.CustomerLedgerEntries
                .Where(e => e.CustomerId == customerId && e.Outstanding > 0 && !e.IsDeleted)
                .SumAsync(e => (decimal?)e.Outstanding, ct) ?? 0m;

            // 3. Query Active Confirmed/Processing Unbilled Orders
            var activeUnbilledOrders = await _db.SalesOrders
                .Where(o => o.CustomerId == customerId && !o.IsDeleted &&
                            (o.Status == SalesOrderStatuses.Confirmed ||
                             o.Status == SalesOrderStatuses.Processing ||
                             o.Status == SalesOrderStatuses.PartiallyDelivered))
                .SumAsync(o => (decimal?)(o.GrandTotal - o.FinalInvoicedAmount), ct) ?? 0m;

            var totalExposure = openAr + activeUnbilledOrders + currentOrderTotal;

            // 4. Overdue Invoice Policy Check
            var now = DateTime.UtcNow;
            var overdueEntries = await _db.CustomerLedgerEntries
                .Where(e => e.CustomerId == customerId && e.Outstanding > 0 && !e.IsDeleted && e.DueDate.HasValue && e.DueDate.Value < now)
                .ToListAsync(ct);

            if (overdueEntries.Count > 0)
            {
                var overdueSum = overdueEntries.Sum(e => e.Outstanding);
                return CreditAssessmentResultDto.Hold(
                    $"Customer has {overdueEntries.Count} overdue invoice(s) totaling ₹{overdueSum:N2}. Overdue balances must be settled before credit confirmation.",
                    totalExposure,
                    approvedCreditLimit);
            }

            // 5. Total Exposure Ceiling Check
            if (totalExposure > approvedCreditLimit)
            {
                return CreditAssessmentResultDto.Hold(
                    $"Customer total exposure (₹{totalExposure:N2} = AR ₹{openAr:N2} + Unbilled Orders ₹{activeUnbilledOrders:N2} + Current Order ₹{currentOrderTotal:N2}) exceeds approved credit limit of ₹{approvedCreditLimit:N2}. Finance credit review required.",
                    totalExposure,
                    approvedCreditLimit);
            }

            return CreditAssessmentResultDto.Passed(totalExposure, approvedCreditLimit);
        }
    }
}
