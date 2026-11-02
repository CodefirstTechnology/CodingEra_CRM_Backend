using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Procurement
{
    public class AccountsPayableAgingService : IAccountsPayableAgingService
    {
        private readonly ERPDbContext _dbContext;

        // Reserve Bank of India Repo Rate baseline ~6.5%; MSMED Section 16 penal rate = 3 * Repo Rate = 19.5% p.a.
        private const decimal MSME_PENAL_RATE_ANNUAL = 0.195m;

        public AccountsPayableAgingService(ERPDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<AccountsPayableAgingGridDto> CalculateAgingScheduleAsync(DateTime asOfDate, int? vendorId = null, CancellationToken ct = default)
        {
            var query = _dbContext.PurchaseBills
                .Where(b => b.Status == PurchaseBillStatus.Posted && b.BalanceAmount > 0);

            if (vendorId.HasValue && vendorId.Value > 0)
            {
                query = query.Where(b => b.VendorId == vendorId.Value);
            }

            var openBills = await query.AsNoTracking().ToListAsync(ct);

            var vendorIds = openBills.Select(b => b.VendorId).Distinct().ToList();
            var vendorsMap = await _dbContext.Vendors
                .Where(v => vendorIds.Contains(v.Id))
                .AsNoTracking()
                .ToDictionaryAsync(v => v.Id, ct);

            var grid = new AccountsPayableAgingGridDto
            {
                AsOfDate = asOfDate
            };

            var vendorGroups = openBills.GroupBy(b => b.VendorId);

            foreach (var group in vendorGroups)
            {
                var sampleBill = group.First();
                vendorsMap.TryGetValue(group.Key, out var vendor);
                var vendorName = vendor?.Name ?? (!string.IsNullOrWhiteSpace(sampleBill.VendorName) ? sampleBill.VendorName : $"Vendor #{group.Key}");
                var msmeCategory = vendor != null ? vendor.MsmeCategory.ToString() : "None";
                bool isMsme = sampleBill.IsMsmeSubjectTo43B || msmeCategory is "Micro" or "Small";

                decimal notDue = 0m;
                decimal days1To15 = 0m;
                decimal days16To30 = 0m;
                decimal days31To45 = 0m;
                decimal daysOver45 = 0m;
                decimal penalInterest = 0m;

                foreach (var bill in group)
                {
                    int creditDays = bill.PaymentTermsDays > 0 ? bill.PaymentTermsDays : 30;
                    DateTime dueDate = bill.DueDate != default ? bill.DueDate : bill.InvoiceDate.AddDays(creditDays);

                    int daysPastDue = (int)(asOfDate.Date - dueDate.Date).TotalDays;

                    if (daysPastDue <= 0)
                    {
                        notDue += bill.OutstandingAmount;
                    }
                    else if (daysPastDue <= 15)
                    {
                        days1To15 += bill.OutstandingAmount;
                    }
                    else if (daysPastDue <= 30)
                    {
                        days16To30 += bill.OutstandingAmount;
                    }
                    else if (daysPastDue <= 45)
                    {
                        days31To45 += bill.OutstandingAmount;
                    }
                    else
                    {
                        daysOver45 += bill.OutstandingAmount;

                        // Project statutory MSME penal interest if applicable
                        if (isMsme)
                        {
                            decimal dailyRate = MSME_PENAL_RATE_ANNUAL / 365m;
                            penalInterest += Math.Round(bill.OutstandingAmount * dailyRate * daysPastDue, 2);
                        }
                    }
                }

                decimal totalOutstanding = notDue + days1To15 + days16To30 + days31To45 + daysOver45;

                var row = new VendorAgingBucketRowDto
                {
                    VendorId = group.Key,
                    VendorName = vendorName,
                    MsmeCategory = msmeCategory,
                    IsMsme43BApplicable = isMsme,
                    TotalOutstanding = totalOutstanding,
                    NotDueAmount = notDue,
                    Days1To15 = days1To15,
                    Days16To30 = days16To30,
                    Days31To45 = days31To45,
                    DaysOver45 = daysOver45,
                    HasOverdueBreach = daysOver45 > 0,
                    ProjectedPenalInterest = penalInterest
                };

                grid.VendorRows.Add(row);

                grid.TotalOutstanding += totalOutstanding;
                grid.TotalNotDue += notDue;
                grid.Total1To15Days += days1To15;
                grid.Total16To30Days += days16To30;
                grid.Total31To45Days += days31To45;
                grid.TotalOver45Days += daysOver45;
                if (isMsme && (days31To45 > 0 || daysOver45 > 0))
                {
                    grid.TotalMsmeRiskAmount += (days31To45 + daysOver45);
                }
            }

            return grid;
        }

        public async Task<List<MsmeComplianceAlertDto>> GetMsmeSection43BAlertsAsync(CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            var msmeBills = await _dbContext.PurchaseBills
                .Where(b => b.Status == PurchaseBillStatus.Posted && b.BalanceAmount > 0 && b.IsMsmeSubjectTo43B)
                .AsNoTracking()
                .ToListAsync(ct);

            var msmeVendorIds = msmeBills.Select(b => b.VendorId).Distinct().ToList();
            var msmeVendorsMap = await _dbContext.Vendors
                .Where(v => msmeVendorIds.Contains(v.Id))
                .AsNoTracking()
                .ToDictionaryAsync(v => v.Id, ct);

            var alerts = new List<MsmeComplianceAlertDto>();

            foreach (var bill in msmeBills)
            {
                msmeVendorsMap.TryGetValue(bill.VendorId, out var vendor);
                var effectiveDueDate = bill.MsmeDueDate ?? bill.InvoiceDate.AddDays(bill.MsmeAllowedDays > 0 ? bill.MsmeAllowedDays : 45);
                int overdueDays = (int)(now.Date - effectiveDueDate.Date).TotalDays;

                if (overdueDays > 0)
                {
                    decimal dailyRate = MSME_PENAL_RATE_ANNUAL / 365m;
                    decimal interest = Math.Round(bill.OutstandingAmount * dailyRate * overdueDays, 2);

                    alerts.Add(new MsmeComplianceAlertDto
                    {
                        PurchaseBillId = bill.Id,
                        BillNumber = bill.BillNumber,
                        VendorId = bill.VendorId,
                        VendorName = vendor?.Name ?? (!string.IsNullOrWhiteSpace(bill.VendorName) ? bill.VendorName : $"Vendor #{bill.VendorId}"),
                        MsmeCategory = vendor != null ? vendor.MsmeCategory.ToString() : "Micro/Small",
                        InvoiceDate = bill.InvoiceDate,
                        MsmeDueDate = effectiveDueDate,
                        AllowedDays = bill.MsmeAllowedDays,
                        OverdueDays = overdueDays,
                        OutstandingAmount = bill.OutstandingAmount,
                        ProjectedInterestAt3xRepoRate = interest,
                        StatutoryRiskLevel = overdueDays > 45 ? "CriticalDisallowance" : "Warning"
                    });
                }
            }

            return alerts.OrderByDescending(a => a.OverdueDays).ToList();
        }
    }
}
