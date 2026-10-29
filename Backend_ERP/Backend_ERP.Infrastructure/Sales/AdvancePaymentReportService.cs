using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    /// <summary>
    /// Advance Payment Report Service:
    ///   - GSTR-1 Table 11A/11B audit report (advance receipt vouchers vs. adjusted invoices)
    ///   - 4-bucket aging schedule (0-15, 16-30, 31-60, 60+ days) on unadjusted float
    /// </summary>
    public class AdvancePaymentReportService : IAdvancePaymentReportService
    {
        private readonly ERPDbContext _db;

        public AdvancePaymentReportService(ERPDbContext db)
        {
            _db = db;
        }

        // ─── GSTR-1 Table 11 Audit Report ────────────────────────────────────────

        public async Task<Gstr1Table11ReportDto> GetGstr1Table11ReportAsync(
            string? monthYear = null,
            string tenantId = "default",
            CancellationToken cancellationToken = default)
        {
            // Parse month-year filter (format: "yyyy-MM")
            DateOnly? periodFrom = null;
            DateOnly? periodTo = null;

            if (!string.IsNullOrWhiteSpace(monthYear)
                && DateOnly.TryParseExact(monthYear + "-01", "yyyy-MM-dd", out var parsedPeriod))
            {
                periodFrom = parsedPeriod;
                periodTo = parsedPeriod.AddMonths(1).AddDays(-1);
            }

            var voucherQuery = _db.AdvancePaymentReceiptVouchers
                .AsNoTracking()
                .Where(v => v.TenantId == tenantId);

            if (periodFrom.HasValue)
            {
                voucherQuery = voucherQuery.Where(v => v.VoucherDate >= periodFrom.Value
                                                    && v.VoucherDate <= periodTo!.Value);
            }

            var vouchers = await voucherQuery
                .Include(v => v.AdvancePayment)
                .OrderBy(v => v.VoucherDate)
                .ToListAsync(cancellationToken);

            var voucherItems = vouchers.Select(v => new Gstr1VoucherItemDto
            {
                VoucherId = v.Id,
                VoucherNumber = v.VoucherNumber,
                PaymentNumber = v.AdvancePayment?.PaymentNumber ?? string.Empty,
                CustomerName = v.CustomerName,
                PlaceOfSupply = v.PlaceOfSupply,
                PlaceOfSupplyStateCode = v.PlaceOfSupplyStateCode,
                IsInterState = v.IsInterState,
                TaxableAmount = v.TaxableAmount,
                CgstAmount = v.CgstAmount,
                SgstAmount = v.SgstAmount,
                IgstAmount = v.IgstAmount,
                TotalTax = v.CgstAmount + v.SgstAmount + v.IgstAmount,
                IsAdjusted = v.IsAdjusted,
                VoucherDate = v.VoucherDate.ToString("yyyy-MM-dd")
            }).ToList();

            var adjustedInInvoices = await _db.AdvancePaymentAdjustments
                .AsNoTracking()
                .Where(a => a.TenantId == tenantId && a.Gstr1VoucherRef != null)
                .SumAsync(a => a.AdjustedAmount, cancellationToken);

            var totalCgst = voucherItems.Sum(v => v.CgstAmount);
            var totalSgst = voucherItems.Sum(v => v.SgstAmount);
            var totalIgst = voucherItems.Sum(v => v.IgstAmount);
            var totalTax = totalCgst + totalSgst + totalIgst;
            var grossAdvanceReceived = voucherItems.Sum(v => v.TaxableAmount + v.TotalTax);

            return new Gstr1Table11ReportDto
            {
                MonthYear = monthYear ?? DateTime.UtcNow.ToString("yyyy-MM"),
                GrossAdvanceReceived = grossAdvanceReceived,
                TotalCgst = totalCgst,
                TotalSgst = totalSgst,
                TotalIgst = totalIgst,
                TotalTax = totalTax,
                AdjustedInInvoices = adjustedInInvoices,
                NetTaxPayableOnAdvances = totalTax - Math.Min(adjustedInInvoices, totalTax),
                Vouchers = voucherItems
            };
        }

        // ─── Unadjusted Float Aging Schedule (4-Bucket) ──────────────────────────

        public async Task<AdvancePaymentAgingReportDto> GetAgingScheduleReportAsync(
            string tenantId = "default",
            CancellationToken cancellationToken = default)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // Unadjusted on-account float: Received or PartiallyApplied with RemainingAmount > 0
            var advances = await _db.AdvancePayments
                .AsNoTracking()
                .Where(ap => !ap.IsDeleted
                          && ap.TenantId == tenantId
                          && ap.RemainingAmount > 0
                          && (ap.Status == AdvancePaymentStatuses.Received
                           || ap.Status == AdvancePaymentStatuses.PartiallyApplied
                           || ap.Status == AdvancePaymentStatuses.Cleared))
                .OrderBy(ap => ap.PaymentDate)
                .ToListAsync(cancellationToken);

            var items = advances.Select(ap =>
            {
                var ageDays = today.DayNumber - ap.PaymentDate.DayNumber;
                var bucket = ageDays switch
                {
                    <= 15 => "0-15 days",
                    <= 30 => "16-30 days",
                    <= 60 => "31-60 days",
                    _ => "60+ days"
                };

                return new AdvancePaymentAgingItemDto
                {
                    PaymentId = ap.Id,
                    PaymentNumber = ap.PaymentNumber,
                    CustomerName = ap.CustomerName,
                    PaymentDate = ap.PaymentDate.ToString("yyyy-MM-dd"),
                    TotalAdvance = ap.AdvanceAmount,
                    RemainingAmount = ap.RemainingAmount,
                    AgeDays = ageDays,
                    AgingBucket = bucket
                };
            }).ToList();

            return new AdvancePaymentAgingReportDto
            {
                GeneratedOn = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                TotalUnadjustedFloat = items.Sum(i => i.RemainingAmount),
                Bucket0To15Days = items.Where(i => i.AgeDays <= 15).Sum(i => i.RemainingAmount),
                Bucket16To30Days = items.Where(i => i.AgeDays is > 15 and <= 30).Sum(i => i.RemainingAmount),
                Bucket31To60Days = items.Where(i => i.AgeDays is > 30 and <= 60).Sum(i => i.RemainingAmount),
                Bucket60PlusDays = items.Where(i => i.AgeDays > 60).Sum(i => i.RemainingAmount),
                Items = items
            };
        }
    }
}

