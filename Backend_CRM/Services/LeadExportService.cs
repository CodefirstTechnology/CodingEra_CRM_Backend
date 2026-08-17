using System.Globalization;
using ClosedXML.Excel;
using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Services
{
    public interface ILeadExportService
    {
        Task<(byte[] Content, string FileName, string? Error)> ExportAsync(
            int userId,
            LeadExportRequestDto request,
            CancellationToken cancellationToken = default);
    }

    public class LeadExportService : ILeadExportService
    {
        private static readonly HashSet<string> SupportedColumnKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "name",
            "source",
            "requirement",
            "status",
            "owner",
            "organization",
            "email",
            "mobile",
            "industry",
            "updated",
            "created",
            "employees",
            "annualRevenue",
            "website",
            "territory",
            "location",
            "leadDate",
            "requestType",
            "notes",
            "gender",
            "dealAmount",
            "gst",
            "salutation",
        };

        private readonly TaskDbcontext _context;
        private readonly IRbacService _rbac;

        public LeadExportService(TaskDbcontext context, IRbacService rbac)
        {
            _context = context;
            _rbac = rbac;
        }

        public async Task<(byte[] Content, string FileName, string? Error)> ExportAsync(
            int userId,
            LeadExportRequestDto request,
            CancellationToken cancellationToken = default)
        {
            request ??= new LeadExportRequestDto();

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
                    out var leadDateFrom,
                    out var leadDateTo,
                    out var dateError))
            {
                return (Array.Empty<byte>(), string.Empty, dateError);
            }

            IQueryable<Lead> q = LeadQueryFilterHelper.QueryWithMasters(_context.Leads.AsNoTracking());
            q = await RbacRecordScopeHelper.ApplyLeadOwnerScopeAsync(_context, _rbac, userId, "leads", q);
            q = await LeadQueryFilterHelper.ApplyListFiltersAsync(
                _context,
                _rbac,
                userId,
                q,
                request.LeadSource,
                request.Status,
                request.LeadOwnerId,
                request.Search,
                leadDateFrom,
                leadDateTo,
                cancellationToken);

            var leads = await q
                .OrderByDescending(l => l.UpdatedAt)
                .AsSplitQuery()
                .ToListAsync(cancellationToken);

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Leads");

            for (var c = 0; c < columns.Count; c++)
            {
                var cell = sheet.Cell(1, c + 1);
                cell.Value = columns[c].Label;
                cell.Style.Font.Bold = true;
            }

            var rowIndex = 2;
            foreach (var lead in leads)
            {
                for (var c = 0; c < columns.Count; c++)
                {
                    WriteCell(sheet.Cell(rowIndex, c + 1), columns[c].Key, lead);
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
            var fileName = $"Leads_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return (stream.ToArray(), fileName, null);
        }

        private static List<(string Key, string Label)> NormalizeColumns(IEnumerable<LeadExportColumnDto>? columns)
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

        private static void WriteCell(IXLCell cell, string key, Lead lead)
        {
            switch (key.ToLowerInvariant())
            {
                case "name":
                    cell.Value = FormatName(lead);
                    break;
                case "source":
                    cell.Value = lead.LeadSource ?? string.Empty;
                    break;
                case "requirement":
                    WriteWrappedText(cell, ToPlainTextForExcel(ResolveRequirement(lead.Notes)));
                    break;
                case "status":
                    cell.Value = lead.LeadStatus?.Name ?? string.Empty;
                    break;
                case "owner":
                    cell.Value = lead.LeadOwner?.FullName ?? string.Empty;
                    break;
                case "organization":
                    cell.Value = lead.Organization?.Name ?? string.Empty;
                    break;
                case "email":
                    cell.Value = lead.Email ?? string.Empty;
                    break;
                case "mobile":
                    cell.Value = lead.Mobile ?? string.Empty;
                    break;
                case "industry":
                    cell.Value = lead.Organization?.Industry?.Name ?? string.Empty;
                    break;
                case "updated":
                    WriteDateTime(cell, lead.UpdatedAt);
                    break;
                case "created":
                    WriteDateTime(cell, lead.CreatedAt);
                    break;
                case "employees":
                    cell.Value = lead.Organization?.EmployeeCount?.Name ?? string.Empty;
                    break;
                case "annualrevenue":
                    if (lead.Organization?.AnnualRevenue is decimal ar)
                    {
                        cell.Value = ar;
                        cell.Style.NumberFormat.Format = "#,##0.00";
                    }
                    else
                    {
                        cell.Value = string.Empty;
                    }

                    break;
                case "website":
                    cell.Value = lead.Organization?.Website ?? string.Empty;
                    break;
                case "territory":
                    cell.Value = lead.Organization?.Territory?.Name ?? string.Empty;
                    break;
                case "location":
                    cell.Value = lead.Location ?? string.Empty;
                    break;
                case "leaddate":
                    WriteDateOnly(cell, lead.LeadDate);
                    break;
                case "requesttype":
                    cell.Value = lead.RequestType?.Name ?? string.Empty;
                    break;
                case "notes":
                    WriteWrappedText(cell, ToPlainTextForExcel(lead.Notes));
                    break;
                case "gender":
                    cell.Value = lead.Gender ?? string.Empty;
                    break;
                case "dealamount":
                    if (lead.DealAmount is decimal da)
                    {
                        cell.Value = da;
                        cell.Style.NumberFormat.Format = "#,##0.00";
                    }
                    else
                    {
                        cell.Value = string.Empty;
                    }

                    break;
                case "gst":
                    cell.Value = lead.Organization?.Gst ?? string.Empty;
                    break;
                case "salutation":
                    cell.Value = lead.Salutation?.Name ?? string.Empty;
                    break;
                default:
                    cell.Value = string.Empty;
                    break;
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
            if (value == null)
            {
                cell.Value = string.Empty;
                return;
            }

            cell.Value = value.Value;
            cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
        }

        private static void WriteWrappedText(IXLCell cell, string value)
        {
            cell.Value = value ?? string.Empty;
            cell.Style.Alignment.WrapText = true;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        }

        private static string FormatName(Lead lead)
        {
            var first = (lead.FirstName ?? string.Empty).Trim();
            var last = (lead.LastName ?? string.Empty).Trim();
            if (first.Length == 0) return last;
            if (last.Length == 0) return first;
            return $"{first} {last}";
        }

        /// <summary>
        /// Mirrors frontend <c>resolveLeadRequirementForDisplay</c> for manual notes
        /// (first paragraph). Marketplace structured notes export the first block as well.
        /// </summary>
        private static string ResolveRequirement(string? notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
            {
                return string.Empty;
            }

            var blocks = notes
                .Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.None)
                .Select(b => b.Trim())
                .Where(b => b.Length > 0)
                .ToList();

            return blocks.Count > 0 ? blocks[0] : notes.Trim();
        }

        /// <summary>
        /// Strips HTML (e.g. IndiaMART <c>&lt;br&gt;</c>) into readable Excel text with real line breaks.
        /// Aligns with frontend <c>plainTextFromHtml</c> / listing display cleanup.
        /// </summary>
        private static string ToPlainTextForExcel(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            var text = raw;
            text = System.Text.RegularExpressions.Regex.Replace(text, @"<br\s*/?>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"</p>\s*", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"<p[^>]*>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"<div[^>]*>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"</div>\s*", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"<[^>]+>", "");

            text = text
                .Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase)
                .Replace("&amp;", "&", StringComparison.OrdinalIgnoreCase)
                .Replace("&lt;", "<", StringComparison.OrdinalIgnoreCase)
                .Replace("&gt;", ">", StringComparison.OrdinalIgnoreCase)
                .Replace("&quot;", "\"", StringComparison.OrdinalIgnoreCase)
                .Replace("&#39;", "'", StringComparison.OrdinalIgnoreCase);

            text = System.Text.RegularExpressions.Regex.Replace(text, @"&#(\d+);", m =>
            {
                if (int.TryParse(m.Groups[1].Value, out var code) && code is >= 0 and <= 0x10FFFF)
                {
                    return char.ConvertFromUtf32(code);
                }

                return string.Empty;
            });

            var lines = text
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Split('\n')
                .Select(line => System.Text.RegularExpressions.Regex.Replace(line, @"\s+", " ").Trim())
                .Where(line => line.Length > 0);

            return string.Join("\n", lines);
        }

        private static string Titleize(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return key;
            var spaced = System.Text.RegularExpressions.Regex.Replace(key, "([a-z])([A-Z])", "$1 $2");
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(spaced.Replace('_', ' ').ToLowerInvariant());
        }
    }
}
