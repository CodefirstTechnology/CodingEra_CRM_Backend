using System;
using System.Collections.Generic;
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
    public class ProformaReportService : IProformaReportService
    {
        private readonly ERPDbContext _db;

        private static readonly Dictionary<string, string> IndianStateMap = new()
        {
            { "01", "Jammu & Kashmir" },
            { "02", "Himachal Pradesh" },
            { "03", "Punjab" },
            { "04", "Chandigarh" },
            { "05", "Uttarakhand" },
            { "06", "Haryana" },
            { "07", "Delhi" },
            { "08", "Rajasthan" },
            { "09", "Uttar Pradesh" },
            { "10", "Bihar" },
            { "19", "West Bengal" },
            { "24", "Gujarat" },
            { "27", "Maharashtra" },
            { "29", "Karnataka" },
            { "32", "Kerala" },
            { "33", "Tamil Nadu" },
            { "36", "Telangana" },
            { "37", "Andhra Pradesh" }
        };

        public ProformaReportService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<ProformaTaxVarianceReportDto>> GetTaxVarianceReportAsync(
            string? dateFrom = null,
            string? dateTo = null,
            CancellationToken ct = default)
        {
            var query = _db.ProformaInvoices
                .AsNoTracking()
                .Include(x => x.Items)
                .Where(x => !x.IsDeleted && x.Status != ProformaInvoiceStatuses.Cancelled);

            if (!string.IsNullOrWhiteSpace(dateFrom) && DateOnly.TryParse(dateFrom, out var dFrom))
            {
                query = query.Where(x => x.InvoiceDate >= dFrom);
            }

            if (!string.IsNullOrWhiteSpace(dateTo) && DateOnly.TryParse(dateTo, out var dTo))
            {
                query = query.Where(x => x.InvoiceDate <= dTo);
            }

            var list = await query.ToListAsync(ct);
            var results = new List<ProformaTaxVarianceReportDto>();

            foreach (var pi in list)
            {
                decimal actualConvertedValue = pi.Items.Sum(i => i.ConvertedQuantity * (i.Rate * (1m - (i.Discount / 100m))));
                decimal variance = actualConvertedValue > 0 ? (pi.GrandTotal - actualConvertedValue) : 0m;
                decimal variancePct = pi.GrandTotal > 0 && actualConvertedValue > 0 ? Math.Round((variance / pi.GrandTotal) * 100m, 2) : 0m;

                results.Add(new ProformaTaxVarianceReportDto
                {
                    ProformaInvoiceId = pi.Id,
                    PiNumber = pi.PiNumber,
                    CustomerName = pi.CustomerName,
                    SalesOrderNumber = pi.SalesOrderNumber,
                    ConvertedInvoiceNumber = pi.ConvertedInvoiceNumber ?? (pi.Items.Any(i => i.ConvertedQuantity > 0) ? "Partial Dispatch" : "Pending"),
                    ProformaGrandTotal = pi.GrandTotal,
                    ActualInvoicedTotal = actualConvertedValue,
                    VarianceAmount = variance,
                    VariancePercentage = variancePct,
                    Reason = actualConvertedValue == 0
                        ? "Goods not yet dispatched or invoiced"
                        : (variance == 0 ? "Exact Match" : "Split dispatch / weighbridge tolerance")
                });
            }

            return results;
        }

        public async Task<IReadOnlyList<ProformaStateTaxProjectionReportDto>> GetStateTaxProjectionReportAsync(
            string? dateFrom = null,
            string? dateTo = null,
            CancellationToken ct = default)
        {
            var query = _db.ProformaInvoices
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.Status != ProformaInvoiceStatuses.Cancelled && x.Status != ProformaInvoiceStatuses.Rejected);

            if (!string.IsNullOrWhiteSpace(dateFrom) && DateOnly.TryParse(dateFrom, out var dFrom))
            {
                query = query.Where(x => x.InvoiceDate >= dFrom);
            }

            if (!string.IsNullOrWhiteSpace(dateTo) && DateOnly.TryParse(dateTo, out var dTo))
            {
                query = query.Where(x => x.InvoiceDate <= dTo);
            }

            var list = await query.ToListAsync(ct);

            // Group by PlaceOfSupplyStateCode (Company home state is 27 Maharashtra)
            const string HomeStateCode = "27";

            var grouped = list
                .GroupBy(x => string.IsNullOrWhiteSpace(x.PlaceOfSupplyStateCode) ? "27" : x.PlaceOfSupplyStateCode.Trim())
                .Select(g =>
                {
                    var stateCode = g.Key;
                    var stateName = IndianStateMap.TryGetValue(stateCode, out var name) ? name : $"State {stateCode}";
                    var count = g.Count();
                    var taxable = g.Sum(x => x.Subtotal - x.DiscountTotal);
                    var totalTax = g.Sum(x => x.TaxTotal);

                    decimal cgst = 0m;
                    decimal sgst = 0m;
                    decimal igst = 0m;

                    if (stateCode == HomeStateCode)
                    {
                        // Intra-state supply: CGST + SGST (50% each)
                        cgst = Math.Round(totalTax / 2m, 2);
                        sgst = totalTax - cgst;
                    }
                    else
                    {
                        // Inter-state supply: IGST 100%
                        igst = totalTax;
                    }

                    return new ProformaStateTaxProjectionReportDto
                    {
                        StateCode = stateCode,
                        StateName = stateName,
                        ProformaCount = count,
                        TaxableAmount = taxable,
                        ProjectedCgst = cgst,
                        ProjectedSgst = sgst,
                        ProjectedIgst = igst,
                        TotalProjectedTax = totalTax
                    };
                })
                .OrderByDescending(x => x.TotalProjectedTax)
                .ToList();

            return grouped;
        }
    }
}
