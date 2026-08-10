using System.Globalization;
using ClosedXML.Excel;
using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Services
{
    public interface IDealExportService
    {
        Task<(byte[] Content, string FileName, string? Error)> ExportAsync(
            int userId,
            DealExportRequestDto request,
            CancellationToken cancellationToken = default);
    }

    public class DealExportService : IDealExportService
    {
        private static readonly HashSet<string> SupportedColumnKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "contactName",
            "name",
            "organizationName",
            "organization",
            "email",
            "mobile",
            "annualRevenue",
            "dealAmount",
            "status",
            "assignedTo",
            "owner",
            "lastModified",
            "updated",
            "created",
            "employees",
            "website",
            "territory",
            "industry",
            "gender",
            "probabilityPercent",
            "nextStep",
            "gst",
            "salutation",
            "nextFollowUpDate",
            "lostReason",
        };

        private readonly TaskDbcontext _context;
        private readonly IRbacService _rbac;

        public DealExportService(TaskDbcontext context, IRbacService rbac)
        {
            _context = context;
            _rbac = rbac;
        }

        public async Task<(byte[] Content, string FileName, string? Error)> ExportAsync(
            int userId,
            DealExportRequestDto request,
            CancellationToken cancellationToken = default)
        {
            request ??= new DealExportRequestDto();

            var columns = NormalizeColumns(request.Columns);
            if (columns.Count == 0)
            {
                return (Array.Empty<byte>(), string.Empty, "Select at least one column to export.");
            }

            if (!LeadDateRangeHelper.TryResolve(
                    request.DatePreset,
                    request.FromDate,
                    request.ToDate,
                    DateTime.Now,
                    out var createdFrom,
                    out var createdTo,
                    out var dateError))
            {
                return (Array.Empty<byte>(), string.Empty, dateError);
            }

            IQueryable<Deal> q = _context.Deals.AsNoTracking()
                .Include(d => d.DealStatus)
                .Include(d => d.AssignedToUser)
                .Include(d => d.DealOwner);

            q = await RbacRecordScopeHelper.ApplyDealOwnerScopeAsync(_context, _rbac, userId, "deals", q);
            q = ApplyListFilters(q, request, createdFrom, createdTo);

            var deals = await q
                .OrderByDescending(d => d.LastModified)
                .ToListAsync(cancellationToken);

            await DealAmountHelper.ApplyLatestQuotationAmountsAsync(_context, deals);

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Deals");

            for (var c = 0; c < columns.Count; c++)
            {
                var cell = sheet.Cell(1, c + 1);
                cell.Value = columns[c].Label;
                cell.Style.Font.Bold = true;
            }

            var rowIndex = 2;
            foreach (var deal in deals)
            {
                for (var c = 0; c < columns.Count; c++)
                {
                    WriteCell(sheet.Cell(rowIndex, c + 1), columns[c].Key, deal);
                }

                rowIndex++;
            }

            var lastCol = columns.Count;
            var lastRow = Math.Max(1, rowIndex - 1);
            var used = sheet.Range(1, 1, lastRow, lastCol);
            used.SetAutoFilter();
            sheet.SheetView.FreezeRows(1);
            sheet.Columns(1, lastCol).AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"Deals_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return (stream.ToArray(), fileName, null);
        }

        private static IQueryable<Deal> ApplyListFilters(
            IQueryable<Deal> q,
            DealExportRequestDto request,
            DateOnly? createdFrom,
            DateOnly? createdTo)
        {
            if (request.StatusId is > 0)
            {
                q = q.Where(d => d.DealStatusId == request.StatusId);
            }
            else if (!string.IsNullOrWhiteSpace(request.Status)
                     && !string.Equals(request.Status.Trim(), "all", StringComparison.OrdinalIgnoreCase))
            {
                var st = request.Status.Trim();
                q = q.Where(d =>
                    d.Status == st
                    || (d.DealStatus != null && d.DealStatus.Name == st));
            }

            if (request.DealOwnerId is > 0)
            {
                var ownerId = request.DealOwnerId.Value;
                q = q.Where(d => d.DealOwnerId == ownerId || d.AssignedToUserId == ownerId);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.Trim().ToLower();
                q = q.Where(d =>
                    (d.FirstName != null && d.FirstName.ToLower().Contains(term))
                    || (d.LastName != null && d.LastName.ToLower().Contains(term))
                    || (d.OrganizationName != null && d.OrganizationName.ToLower().Contains(term))
                    || (d.Email != null && d.Email.ToLower().Contains(term))
                    || (d.Mobile != null && d.Mobile.ToLower().Contains(term))
                    || (d.Status != null && d.Status.ToLower().Contains(term))
                    || (d.DealStatus != null && d.DealStatus.Name.ToLower().Contains(term))
                    || (d.NextStep != null && d.NextStep.ToLower().Contains(term))
                    || (d.DealOwner != null && d.DealOwner.FullName.ToLower().Contains(term))
                    || (d.AssignedToUser != null && d.AssignedToUser.FullName.ToLower().Contains(term)));
            }

            if (createdFrom != null)
            {
                var fromDt = createdFrom.Value.ToDateTime(TimeOnly.MinValue);
                q = q.Where(d => d.CreatedAt >= fromDt);
            }

            if (createdTo != null)
            {
                var toExclusive = createdTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
                q = q.Where(d => d.CreatedAt < toExclusive);
            }

            return q;
        }

        private static List<(string Key, string Label)> NormalizeColumns(IEnumerable<DealExportColumnDto>? columns)
        {
            var result = new List<(string Key, string Label)>();
            if (columns == null)
            {
                return result;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var col in columns)
            {
                var key = (col.Key ?? string.Empty).Trim();
                if (key.Length == 0 || !SupportedColumnKeys.Contains(key) || !seen.Add(key))
                {
                    continue;
                }

                var label = string.IsNullOrWhiteSpace(col.Label) ? Titleize(key) : col.Label.Trim();
                result.Add((key, label));
            }

            return result;
        }

        private static void WriteCell(IXLCell cell, string key, Deal deal)
        {
            switch (key.ToLowerInvariant())
            {
                case "contactname":
                case "name":
                    cell.Value = FormatName(deal);
                    break;
                case "organizationname":
                case "organization":
                    cell.Value = deal.OrganizationName ?? string.Empty;
                    break;
                case "email":
                    cell.Value = deal.Email ?? string.Empty;
                    break;
                case "mobile":
                    cell.Value = deal.Mobile ?? string.Empty;
                    break;
                case "annualrevenue":
                    WriteDecimal(cell, deal.AnnualRevenue);
                    break;
                case "dealamount":
                    WriteDecimal(cell, deal.DealAmount);
                    break;
                case "status":
                    cell.Value = deal.DealStatus?.Name ?? deal.Status ?? string.Empty;
                    break;
                case "assignedto":
                case "owner":
                    cell.Value = deal.DealOwner?.FullName
                        ?? deal.AssignedToUser?.FullName
                        ?? string.Empty;
                    break;
                case "lastmodified":
                case "updated":
                    WriteDateTime(cell, deal.LastModified != default ? deal.LastModified : deal.UpdatedAt);
                    break;
                case "created":
                    WriteDateTime(cell, deal.CreatedAt);
                    break;
                case "employees":
                    cell.Value = deal.Employees ?? string.Empty;
                    break;
                case "website":
                    cell.Value = deal.Website ?? string.Empty;
                    break;
                case "territory":
                    cell.Value = deal.Territory ?? string.Empty;
                    break;
                case "industry":
                    cell.Value = deal.Industry ?? string.Empty;
                    break;
                case "gender":
                    cell.Value = deal.Gender ?? string.Empty;
                    break;
                case "probabilitypercent":
                    if (deal.ProbabilityPercent is int pct)
                    {
                        cell.Value = pct;
                    }
                    else
                    {
                        cell.Value = string.Empty;
                    }

                    break;
                case "nextstep":
                    cell.Value = deal.NextStep ?? string.Empty;
                    break;
                case "gst":
                    cell.Value = deal.Gst ?? string.Empty;
                    break;
                case "salutation":
                    cell.Value = deal.Salutation ?? string.Empty;
                    break;
                case "nextfollowupdate":
                    WriteDateOnly(cell, deal.NextFollowUpDate);
                    break;
                case "lostreason":
                    cell.Value = deal.LostReason ?? string.Empty;
                    break;
                default:
                    cell.Value = string.Empty;
                    break;
            }
        }

        private static void WriteDecimal(IXLCell cell, decimal? value)
        {
            if (value is decimal d)
            {
                cell.Value = d;
                cell.Style.NumberFormat.Format = "#,##0.00";
            }
            else
            {
                cell.Value = string.Empty;
            }
        }

        private static void WriteDateOnly(IXLCell cell, DateTime? value)
        {
            if (value == null)
            {
                cell.Value = string.Empty;
                return;
            }

            cell.Value = value.Value.Date;
            cell.Style.DateFormat.Format = "yyyy-mm-dd";
        }

        private static void WriteDateTime(IXLCell cell, DateTime? value)
        {
            if (value == null || value == default)
            {
                cell.Value = string.Empty;
                return;
            }

            cell.Value = value.Value;
            cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
        }

        private static string FormatName(Deal deal)
        {
            var first = (deal.FirstName ?? string.Empty).Trim();
            var last = (deal.LastName ?? string.Empty).Trim();
            if (first.Length == 0) return last;
            if (last.Length == 0) return first;
            return $"{first} {last}";
        }

        private static string Titleize(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return key;
            var spaced = System.Text.RegularExpressions.Regex.Replace(key, "([a-z])([A-Z])", "$1 $2");
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(spaced.Replace('_', ' ').ToLowerInvariant());
        }
    }
}
