using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class PerformanceService : IPerformanceService
    {
        private readonly IPerformanceRepository _repo;

        public PerformanceService(IPerformanceRepository repo)
        {
            _repo = repo;
        }

        public async Task<PerformanceDashboardDto> GetDashboardAsync(
            PerformanceFilterDto filter,
            CancellationToken cancellationToken = default)
        {
            var rows = await BuildSalespersonsAsync(filter, cancellationToken);
            return new PerformanceDashboardDto
            {
                GeneratedOn = DateTimeOffset.UtcNow.UtcDateTime.ToString("O"),
                Summary = Summarize(rows),
                Salespersons = rows,
                Trends = await BuildTrendsAsync(null, filter, false, cancellationToken)
            };
        }

        public async Task<IReadOnlyList<PerformanceLeaderboardDto>> GetLeaderboardsAsync(
            PerformanceFilterDto filter,
            CancellationToken cancellationToken = default)
        {
            var rows = await BuildSalespersonsAsync(filter, cancellationToken);
            return rows
                .OrderByDescending(x => x.RevenueAchievementPercentage)
                .ThenByDescending(x => x.SalesRevenue)
                .Select((x, index) => new PerformanceLeaderboardDto
                {
                    Rank = index + 1,
                    SalesPersonUserId = x.SalesPersonUserId,
                    SalesPerson = x.SalesPerson,
                    AchievementPercentage = x.RevenueAchievementPercentage,
                    SalesRevenue = x.SalesRevenue,
                    IsTopPerformer = index < 3
                }).ToList();
        }

        public async Task<IReadOnlyList<PerformanceSalesPersonDto>> GetSalespersonsAsync(
            PerformanceFilterDto filter,
            CancellationToken cancellationToken = default) =>
            await BuildSalespersonsAsync(filter, cancellationToken);

        public async Task<PerformanceSalesPersonDto?> GetSalespersonAsync(
            int id,
            PerformanceFilterDto filter,
            CancellationToken cancellationToken = default)
        {
            ValidateSalesperson(id);
            filter.SalesPersonUserId = id;
            return (await BuildSalespersonsAsync(filter, cancellationToken)).FirstOrDefault();
        }

        public async Task<IReadOnlyList<PerformanceTimelineDto>> GetTimelineAsync(
            int id,
            PerformanceFilterDto filter,
            CancellationToken cancellationToken = default)
        {
            ValidateSalesperson(id);
            var range = ResolveRange(filter);
            var orders = await _repo.SalesOrders().AsNoTracking().ToListAsync(cancellationToken);
            var orderIds = orders.Where(x => UserId(x.SalesPerson, x.CreatedBy) == id)
                .Select(x => x.Id).ToHashSet();
            var orderNumbers = orders.Where(x => orderIds.Contains(x.Id))
                .Select(x => x.SalesOrderNumber).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var events = orders
                .Where(x => orderIds.Contains(x.Id) && InRange(x.OrderDate, range))
                .Select(x => new PerformanceTimelineDto
                {
                    Date = x.OrderDate.ToString("yyyy-MM-dd"),
                    EventType = "SalesOrder",
                    ReferenceNumber = x.SalesOrderNumber,
                    Description = $"{x.Status} sales order",
                    Value = x.GrandTotal
                }).ToList();

            var proformas = await _repo.ProformaInvoices().AsNoTracking()
                .Where(x => x.SalesPersonUserId == id).ToListAsync(cancellationToken);
            events.AddRange(proformas.Where(x => InRange(x.InvoiceDate, range)).Select(x => new PerformanceTimelineDto
            {
                Date = x.InvoiceDate.ToString("yyyy-MM-dd"),
                EventType = "ProformaInvoice",
                ReferenceNumber = x.PiNumber,
                Description = $"{x.Status} proforma invoice",
                Value = x.GrandTotal
            }));

            var advances = await _repo.AdvancePayments().AsNoTracking().ToListAsync(cancellationToken);
            events.AddRange(advances
                .Where(x => InRange(x.PaymentDate, range)
                    && ((x.SalesOrderId != null && orderIds.Contains(x.SalesOrderId.Value))
                        || orderNumbers.Contains(x.SalesOrderNumber)))
                .Select(x => new PerformanceTimelineDto
                {
                    Date = x.PaymentDate.ToString("yyyy-MM-dd"),
                    EventType = "AdvancePayment",
                    ReferenceNumber = x.PaymentNumber,
                    Description = $"{x.Status} advance payment",
                    Value = x.AdvanceAmount
                }));

            return events.OrderByDescending(x => x.Date).ToList();
        }

        public async Task<IReadOnlyList<PerformanceTrendDto>> GetMonthlyAsync(
            int id,
            PerformanceFilterDto filter,
            CancellationToken cancellationToken = default)
        {
            ValidateSalesperson(id);
            return await BuildTrendsAsync(id, filter, false, cancellationToken);
        }

        public async Task<IReadOnlyList<PerformanceHistoryDto>> GetPerformanceHistoryAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            ValidateSalesperson(id);
            var rows = await _repo.History().AsNoTracking()
                .Where(x => x.SalesPersonUserId == id)
                .OrderByDescending(x => x.RecordedOn)
                .ToListAsync(cancellationToken);
            return rows.Select(ToHistory).ToList();
        }

        public async Task<IReadOnlyList<PerformanceHistoryDto>> GetTargetHistoryAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            ValidateSalesperson(id);
            var stored = await _repo.History().AsNoTracking()
                .Where(x => x.SalesPersonUserId == id && x.MetricName.ToLower().Contains("target"))
                .OrderByDescending(x => x.RecordedOn)
                .ToListAsync(cancellationToken);
            return stored.Select(ToHistory).ToList();
        }

        public async Task<PerformanceAnalyticsDto> GetAnalyticsAsync(
            PerformanceFilterDto filter,
            CancellationToken cancellationToken = default)
        {
            var rows = await BuildSalespersonsAsync(filter, cancellationToken);
            var ranked = await GetLeaderboardsAsync(filter, cancellationToken);
            return new PerformanceAnalyticsDto
            {
                Summary = Summarize(rows),
                MonthlyTrends = await BuildTrendsAsync(filter.SalesPersonUserId, filter, false, cancellationToken),
                QuarterlyTrends = await BuildTrendsAsync(filter.SalesPersonUserId, filter, true, cancellationToken),
                TopPerformers = ranked.Take(5).ToList(),
                BottomPerformers = ranked.Reverse().Take(5).ToList()
            };
        }

        public async Task<PerformanceReportDto> GetReportsAsync(
            PerformanceFilterDto filter,
            CancellationToken cancellationToken = default)
        {
            var rows = await BuildSalespersonsAsync(filter, cancellationToken);
            return new PerformanceReportDto
            {
                GeneratedOn = DateTimeOffset.UtcNow.UtcDateTime.ToString("O"),
                Summary = Summarize(rows),
                Rows = rows
            };
        }

        public async Task<PerformanceExportDto> ExportAsync(
            PerformanceExportRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            _ = await GetReportsAsync(request.Filters ?? new PerformanceFilterDto(), cancellationToken);
            var type = request.ExportType.Trim().ToLowerInvariant() is "xlsx" ? "xlsx" : "csv";
            var now = DateTimeOffset.UtcNow;
            var export = new PerformanceExportHistory
            {
                ReportName = string.IsNullOrWhiteSpace(request.ReportName)
                    ? "performance-report"
                    : request.ReportName.Trim(),
                GeneratedBy = actingUser,
                GeneratedOn = now,
                FileName = $"performance-report-{DateTime.UtcNow:yyyyMMddHHmmss}.{type}",
                ExportType = type
            };
            await _repo.AddExportAsync(export, cancellationToken);
            return ToExport(export);
        }

        public async Task<IReadOnlyList<PerformanceExportDto>> GetExportHistoryAsync(
            CancellationToken cancellationToken = default)
        {
            var rows = await _repo.ExportHistory().AsNoTracking()
                .OrderByDescending(x => x.GeneratedOn).Take(100).ToListAsync(cancellationToken);
            return rows.Select(ToExport).ToList();
        }

        public async Task<IReadOnlyList<PerformanceLookupDto>> LookupSalespersonsAsync(
            CancellationToken cancellationToken = default)
        {
            var targets = await _repo.SalesTargets().AsNoTracking()
                .Where(x => x.SalesPersonUserId != null).Select(x => x.SalesPersonUserId!.Value)
                .ToListAsync(cancellationToken);
            var proformas = await _repo.ProformaInvoices().AsNoTracking()
                .Select(x => x.SalesPersonUserId).ToListAsync(cancellationToken);
            var orders = await _repo.SalesOrders().AsNoTracking()
                .Select(x => new { x.SalesPerson, x.CreatedBy }).ToListAsync(cancellationToken);
            return targets.Concat(proformas)
                .Concat(orders.Select(x => UserId(x.SalesPerson, x.CreatedBy)))
                .Where(x => x > 0).Concat([1, 2, 3]).Distinct().OrderBy(x => x)
                .Select(x => new PerformanceLookupDto { Id = x.ToString(), Name = $"Salesperson {x}" })
                .ToList();
        }

        public Task<IReadOnlyList<PerformanceLookupDto>> LookupTeamsAsync(
            CancellationToken cancellationToken = default) => LookupDimensionAsync(
                _repo.SalesTargets().Select(x => x.SalesTeam), cancellationToken);

        public Task<IReadOnlyList<PerformanceLookupDto>> LookupBranchesAsync(
            CancellationToken cancellationToken = default) => LookupDimensionAsync(
                _repo.SalesTargets().Select(x => x.Branch), cancellationToken);

        public Task<IReadOnlyList<PerformanceLookupDto>> LookupRegionalManagersAsync(
            CancellationToken cancellationToken = default) => LookupDimensionAsync(
                _repo.SalesTargets().Select(x => x.RegionalManager), cancellationToken);

        public Task<IReadOnlyList<string>> GetPermissionsAsync() =>
            Task.FromResult<IReadOnlyList<string>>(
            [
                "performance-dashboard.view",
                "performance-dashboard.leaderboards.view",
                "performance-dashboard.analytics.view",
                "performance-dashboard.reports.view",
                "performance-dashboard.reports.export"
            ]);

        private async Task<List<PerformanceSalesPersonDto>> BuildSalespersonsAsync(
            PerformanceFilterDto filter,
            CancellationToken cancellationToken)
        {
            var range = ResolveRange(filter);
            var orders = await _repo.SalesOrders().AsNoTracking().ToListAsync(cancellationToken);
            var proformas = await _repo.ProformaInvoices().AsNoTracking().ToListAsync(cancellationToken);
            var advances = await _repo.AdvancePayments().AsNoTracking().ToListAsync(cancellationToken);
            var targets = await _repo.SalesTargets().AsNoTracking().ToListAsync(cancellationToken);

            targets = targets.Where(x => TargetMatches(x, filter, range)).ToList();
            var ids = orders.Select(x => UserId(x.SalesPerson, x.CreatedBy))
                .Concat(proformas.Select(x => x.SalesPersonUserId))
                .Concat(targets.Where(x => x.SalesPersonUserId != null).Select(x => x.SalesPersonUserId!.Value))
                .Where(x => x > 0).Distinct().ToList();
            if (filter.SalesPersonUserId is int wanted)
            {
                ids = ids.Where(x => x == wanted).ToList();
            }

            var result = new List<PerformanceSalesPersonDto>();
            foreach (var id in ids)
            {
                var personOrders = orders.Where(x =>
                    UserId(x.SalesPerson, x.CreatedBy) == id && InRange(x.OrderDate, range)).ToList();
                var orderIds = personOrders.Select(x => x.Id).ToHashSet();
                var orderNumbers = personOrders.Select(x => x.SalesOrderNumber)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var personPis = proformas.Where(x =>
                    x.SalesPersonUserId == id && InRange(x.InvoiceDate, range)).ToList();
                var personAdvances = advances.Where(x =>
                    InRange(x.PaymentDate, range)
                    && ((x.SalesOrderId != null && orderIds.Contains(x.SalesOrderId.Value))
                        || orderNumbers.Contains(x.SalesOrderNumber))).ToList();
                var personTargets = targets.Where(x => x.SalesPersonUserId == id).ToList();
                var dimension = personTargets.OrderByDescending(x => x.UpdatedDate).FirstOrDefault();

                var completed = personOrders.Count(x => x.Status == SalesOrderStatuses.Completed);
                var salesRevenue = personOrders.Where(x =>
                    x.Status is SalesOrderStatuses.Confirmed
                        or SalesOrderStatuses.Processing
                        or SalesOrderStatuses.PartiallyDelivered
                        or SalesOrderStatuses.Completed).Sum(x => x.GrandTotal);
                var approvedPi = personPis.Count(x =>
                    x.Status is ProformaInvoiceStatuses.Approved
                        or ProformaInvoiceStatuses.Sent
                        or ProformaInvoiceStatuses.Accepted
                        or ProformaInvoiceStatuses.Converted);
                var convertedPi = personPis.Count(x => x.Status == ProformaInvoiceStatuses.Converted);
                var assigned = personTargets.Sum(x => x.TargetValue);
                var achieved = personTargets.Sum(x => x.AchievedValue);
                var salesOrderTarget = personTargets
                    .Where(x => x.TargetCategory == SalesTargetCategories.SalesOrder).Sum(x => x.TargetValue);
                var revenueTarget = personTargets
                    .Where(x => x.TargetCategory == SalesTargetCategories.Revenue).Sum(x => x.TargetValue);
                var received = personAdvances.Where(x =>
                    x.Status is AdvancePaymentStatuses.Received
                        or AdvancePaymentStatuses.PartiallyApplied
                        or AdvancePaymentStatuses.FullyApplied).Sum(x => x.AdvanceAmount);

                result.Add(new PerformanceSalesPersonDto
                {
                    SalesPersonUserId = id,
                    SalesPerson = personOrders.Select(x => x.SalesPerson)
                        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? $"Salesperson {id}",
                    SalesTeam = dimension?.SalesTeam ?? string.Empty,
                    Branch = dimension?.Branch ?? string.Empty,
                    RegionalManager = dimension?.RegionalManager ?? string.Empty,
                    TotalSalesOrders = personOrders.Count,
                    ConfirmedSalesOrders = personOrders.Count(x => x.Status == SalesOrderStatuses.Confirmed),
                    CompletedSalesOrders = completed,
                    CancelledSalesOrders = personOrders.Count(x => x.Status == SalesOrderStatuses.Cancelled),
                    SalesRevenue = salesRevenue,
                    ProformaInvoiceCount = personPis.Count,
                    ApprovedProformaInvoices = approvedPi,
                    ConvertedProformaInvoices = convertedPi,
                    ProformaAmount = personPis.Sum(x => x.GrandTotal),
                    AdvanceReceived = received,
                    AdvanceApplied = personAdvances.Sum(x => x.AppliedAmount),
                    OutstandingAdvance = personAdvances.Sum(x => x.RemainingAmount),
                    AssignedTarget = assigned,
                    AchievedTarget = achieved,
                    SalesAchievementPercentage = PerformanceCalculator.Percentage(completed, salesOrderTarget),
                    RevenueAchievementPercentage = PerformanceCalculator.Percentage(salesRevenue, revenueTarget),
                    CollectionPercentage = PerformanceCalculator.Percentage(received, salesRevenue),
                    ConversionPercentage = PerformanceCalculator.Percentage(convertedPi, approvedPi)
                });
            }

            return result.Where(x =>
                Match(x.SalesTeam, filter.SalesTeam)
                && Match(x.Branch, filter.Branch)
                && Match(x.RegionalManager, filter.RegionalManager)).ToList();
        }

        private async Task<List<PerformanceTrendDto>> BuildTrendsAsync(
            int? salesPersonId,
            PerformanceFilterDto filter,
            bool quarterly,
            CancellationToken cancellationToken)
        {
            var range = ResolveRange(filter);
            var orders = await _repo.SalesOrders().AsNoTracking().ToListAsync(cancellationToken);
            var targets = await _repo.SalesTargets().AsNoTracking().ToListAsync(cancellationToken);
            var advances = await _repo.AdvancePayments().AsNoTracking().ToListAsync(cancellationToken);
            var months = Enumerable.Range(0, 12)
                .Select(offset => range.Start.AddMonths(offset))
                .Where(x => x <= range.End).ToList();

            return months.GroupBy(x => quarterly ? $"{x.Year}-Q{((x.Month - 1) / 3) + 1}" : $"{x:yyyy-MM}")
                .Select(group =>
                {
                    var dates = group.ToList();
                    var start = dates.Min();
                    var end = quarterly ? start.AddMonths(3).AddDays(-1) : start.AddMonths(1).AddDays(-1);
                    var revenue = orders.Where(x =>
                        (!salesPersonId.HasValue || UserId(x.SalesPerson, x.CreatedBy) == salesPersonId)
                        && x.OrderDate >= start && x.OrderDate <= end
                        && x.Status == SalesOrderStatuses.Completed).Sum(x => x.GrandTotal);
                    var target = targets.Where(x =>
                        (!salesPersonId.HasValue || x.SalesPersonUserId == salesPersonId)
                        && x.TargetCategory == SalesTargetCategories.Revenue
                        && x.StartDate <= end && x.EndDate >= start).Sum(x => x.TargetValue);
                    var collections = advances.Where(x => x.PaymentDate >= start && x.PaymentDate <= end)
                        .Sum(x => x.AdvanceAmount);
                    return new PerformanceTrendDto
                    {
                        Period = group.Key,
                        PeriodType = quarterly ? PerformancePeriodTypes.Quarterly : PerformancePeriodTypes.Monthly,
                        SalesRevenue = revenue,
                        Collections = collections,
                        Target = target,
                        AchievementPercentage = PerformanceCalculator.Percentage(revenue, target)
                    };
                }).ToList();
        }

        private static PerformanceSummaryDto Summarize(IReadOnlyCollection<PerformanceSalesPersonDto> rows)
        {
            var revenue = rows.Sum(x => x.SalesRevenue);
            var revenueTarget = rows.Sum(x => x.AssignedTarget);
            var approved = rows.Sum(x => x.ApprovedProformaInvoices);
            var converted = rows.Sum(x => x.ConvertedProformaInvoices);
            var received = rows.Sum(x => x.AdvanceReceived);
            return new PerformanceSummaryDto
            {
                TotalSalesOrders = rows.Sum(x => x.TotalSalesOrders),
                ConfirmedSalesOrders = rows.Sum(x => x.ConfirmedSalesOrders),
                CompletedSalesOrders = rows.Sum(x => x.CompletedSalesOrders),
                CancelledSalesOrders = rows.Sum(x => x.CancelledSalesOrders),
                SalesRevenue = revenue,
                ProformaInvoiceCount = rows.Sum(x => x.ProformaInvoiceCount),
                ApprovedProformaInvoices = approved,
                ConvertedProformaInvoices = converted,
                ProformaAmount = rows.Sum(x => x.ProformaAmount),
                AdvanceReceived = received,
                AdvanceApplied = rows.Sum(x => x.AdvanceApplied),
                OutstandingAdvance = rows.Sum(x => x.OutstandingAdvance),
                AssignedTarget = revenueTarget,
                AchievedTarget = rows.Sum(x => x.AchievedTarget),
                SalesAchievementPercentage = rows.Count == 0 ? 0 : Math.Round(rows.Average(x => x.SalesAchievementPercentage), 2),
                RevenueAchievementPercentage = PerformanceCalculator.Percentage(revenue, revenueTarget),
                CollectionPercentage = PerformanceCalculator.Percentage(received, revenue),
                ConversionPercentage = PerformanceCalculator.Percentage(converted, approved)
            };
        }

        private static (DateOnly Start, DateOnly End) ResolveRange(PerformanceFilterDto filter)
        {
            if (filter.Month is < 1 or > 12)
                throw new InvalidOperationException("Month must be between 1 and 12.");
            if (filter.Quarter is < 1 or > 4)
                throw new InvalidOperationException("Quarter must be between 1 and 4.");
            if (filter.FinancialYear is < 2000 or > 9999)
                throw new InvalidOperationException("Financial year is invalid.");
            if (!string.IsNullOrWhiteSpace(filter.PeriodType)
                && !PerformancePeriodTypes.All.Contains(filter.PeriodType, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Unknown period '{filter.PeriodType}'.");

            var year = filter.FinancialYear ?? DateTime.UtcNow.Year;
            DateOnly start;
            DateOnly end;
            if (filter.Month is int month)
            {
                start = new DateOnly(year, month, 1);
                end = start.AddMonths(1).AddDays(-1);
            }
            else if (filter.Quarter is int quarter)
            {
                start = new DateOnly(year, (quarter - 1) * 3 + 1, 1);
                end = start.AddMonths(3).AddDays(-1);
            }
            else
            {
                start = new DateOnly(year, 1, 1);
                end = new DateOnly(year, 12, 31);
            }

            if (!string.IsNullOrWhiteSpace(filter.DateFrom)
                && !DateOnly.TryParse(filter.DateFrom, out start))
                throw new InvalidOperationException("Date from is invalid.");
            if (!string.IsNullOrWhiteSpace(filter.DateTo)
                && !DateOnly.TryParse(filter.DateTo, out end))
                throw new InvalidOperationException("Date to is invalid.");
            if (end < start)
                throw new InvalidOperationException("Date to must be on or after date from.");
            return (start, end);
        }

        private static bool TargetMatches(
            SalesTarget target,
            PerformanceFilterDto filter,
            (DateOnly Start, DateOnly End) range) =>
            target.StartDate <= range.End && target.EndDate >= range.Start
            && (!filter.FinancialYear.HasValue || target.FinancialYear == filter.FinancialYear)
            && Match(target.SalesTeam, filter.SalesTeam)
            && Match(target.Branch, filter.Branch)
            && Match(target.RegionalManager, filter.RegionalManager);

        private static bool InRange(DateOnly date, (DateOnly Start, DateOnly End) range) =>
            date >= range.Start && date <= range.End;

        private static bool Match(string value, string? filter) =>
            string.IsNullOrWhiteSpace(filter)
            || value.Equals(filter.Trim(), StringComparison.OrdinalIgnoreCase);

        private static int UserId(string? salesperson, string? fallback)
        {
            if (int.TryParse(salesperson?.Replace("user:", "", StringComparison.OrdinalIgnoreCase), out var id) && id > 0)
                return id;
            if (int.TryParse(fallback?.Replace("user:", "", StringComparison.OrdinalIgnoreCase), out id) && id > 0)
                return id;
            return 0;
        }

        private static void ValidateSalesperson(int id)
        {
            if (id <= 0)
                throw new InvalidOperationException("Salesperson is invalid.");
        }

        private async Task<IReadOnlyList<PerformanceLookupDto>> LookupDimensionAsync(
            IQueryable<string> query,
            CancellationToken cancellationToken)
        {
            var values = await query.AsNoTracking().Where(x => x != "")
                .Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
            return values.Select(x => new PerformanceLookupDto { Id = x, Name = x }).ToList();
        }

        private static PerformanceHistoryDto ToHistory(PerformanceHistory x) => new()
        {
            Id = x.Id,
            SalesPersonUserId = x.SalesPersonUserId,
            PeriodStart = x.PeriodStart.ToString("yyyy-MM-dd"),
            PeriodEnd = x.PeriodEnd.ToString("yyyy-MM-dd"),
            MetricName = x.MetricName,
            OldValue = x.OldValue,
            NewValue = x.NewValue,
            Remarks = x.Remarks,
            RecordedOn = x.RecordedOn.UtcDateTime.ToString("O")
        };

        private static PerformanceExportDto ToExport(PerformanceExportHistory x) => new()
        {
            Id = x.Id,
            ReportName = x.ReportName,
            GeneratedBy = x.GeneratedBy,
            GeneratedOn = x.GeneratedOn.UtcDateTime.ToString("O"),
            FileName = x.FileName,
            ExportType = x.ExportType
        };
    }
}
