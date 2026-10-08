using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using CRM.DATA;
using CRM.DTO;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Services
{
    public class LeadTrackerService : ILeadTrackerService
    {
        private readonly TaskDbcontext _context;

        public LeadTrackerService(TaskDbcontext context)
        {
            _context = context;
        }

        public async Task<LeadTrackerPagedResultDto> GetPagedTrackerDataAsync(LeadTrackerQueryParams filters)
        {
            var baseQuery = _context.VwLeadTrackers.AsNoTracking();
            var filteredQuery = ApplyFilters(baseQuery, filters);

            // Total count for current filter
            var totalCount = await filteredQuery.CountAsync();

            // Compute Executive Summary Metrics across filtered scope
            var summary = new LeadTrackerSummaryDto
            {
                TotalLeads = totalCount,
                OpenLeads = await filteredQuery.CountAsync(r => r.Status == "Open"),
                WonLeads = await filteredQuery.CountAsync(r => r.Status == "Won"),
                LostLeads = await filteredQuery.CountAsync(r => r.Status == "Lost"),
                TotalEstimatedValue = await filteredQuery.SumAsync(r => (decimal?)r.EstimatedLeadValue) ?? 0m,
                TotalQuotationValue = await filteredQuery.SumAsync(r => (decimal?)r.QuotationValue) ?? 0m,
                TotalWonValue = await filteredQuery.Where(r => r.Status == "Won").SumAsync(r => (decimal?)r.OrderValue) ?? 0m
            };

            // Apply sorting
            var sortedQuery = ApplySorting(filteredQuery, filters.SortBy, filters.SortDirection);

            var pageIndex = filters.PageIndex > 0 ? filters.PageIndex : 1;
            var pageSize = filters.PageSize > 0 ? filters.PageSize : 25;

            // Fetch raw rows synchronously into memory before mapping to DTO to avoid EF translation errors
            var rawRows = await sortedQuery
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = rawRows.Select(MapToDto).ToList();

            return new LeadTrackerPagedResultDto
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize,
                Summary = summary
            };
        }

        public async Task<byte[]> GenerateExcelExportAsync(LeadTrackerQueryParams filters)
        {
            var baseQuery = _context.VwLeadTrackers.AsNoTracking();
            var filteredQuery = ApplyFilters(baseQuery, filters);
            var sortedQuery = ApplySorting(filteredQuery, filters.SortBy, filters.SortDirection);

            var rawRows = await sortedQuery.ToListAsync();
            var data = rawRows.Select(MapToDto).ToList();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Lead Tracker Matrix");

            var headers = new[]
            {
                "Lead ID", "Customer Name", "Lead Date", "Month", "Sales Executive",
                "Lead Source", "Contact Person", "Mobile", "Requirement / Product", "Est. Lead Value (₹)",
                "First Contact Date", "Contacted?", "Qualified?", "Meeting Date", "Meeting Done?",
                "Quotation Date", "Quotation Value (₹)", "Last Follow-up", "Next Follow-up", "Follow-up SLA",
                "Current Stage", "Status", "Days Open", "Ageing Bucket", "Expected Order Date",
                "Order Date", "Order Value (₹)", "Lost Reason"
            };

            for (int col = 0; col < headers.Length; col++)
            {
                var cell = sheet.Cell(1, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F172A");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int rowIdx = 2;
            foreach (var r in data)
            {
                sheet.Cell(rowIdx, 1).Value = r.LeadId;
                sheet.Cell(rowIdx, 2).Value = r.CustomerName;
                sheet.Cell(rowIdx, 3).Value = r.LeadDate.HasValue ? r.LeadDate.Value.ToString("dd-MMM-yyyy") : "N/A";
                sheet.Cell(rowIdx, 4).Value = r.MonthFormatted;
                sheet.Cell(rowIdx, 5).Value = r.SalesExecutive;
                sheet.Cell(rowIdx, 6).Value = r.LeadSource;
                sheet.Cell(rowIdx, 7).Value = r.ContactPerson;
                sheet.Cell(rowIdx, 8).Value = r.Mobile;
                sheet.Cell(rowIdx, 9).Value = r.RequirementProduct;
                sheet.Cell(rowIdx, 10).Value = r.EstimatedLeadValue;
                sheet.Cell(rowIdx, 11).Value = r.FirstContactDate.HasValue ? r.FirstContactDate.Value.ToString("dd-MMM-yyyy") : "N/A";
                sheet.Cell(rowIdx, 12).Value = r.Contacted;
                sheet.Cell(rowIdx, 13).Value = r.Qualified;
                sheet.Cell(rowIdx, 14).Value = r.MeetingDate.HasValue ? r.MeetingDate.Value.ToString("dd-MMM-yyyy") : "N/A";
                sheet.Cell(rowIdx, 15).Value = r.MeetingDone;
                sheet.Cell(rowIdx, 16).Value = r.QuotationDate.HasValue ? r.QuotationDate.Value.ToString("dd-MMM-yyyy") : "N/A";
                sheet.Cell(rowIdx, 17).Value = r.QuotationValue;
                sheet.Cell(rowIdx, 18).Value = r.LastFollowUpDate.HasValue ? r.LastFollowUpDate.Value.ToString("dd-MMM-yyyy") : "N/A";
                sheet.Cell(rowIdx, 19).Value = r.NextFollowUpDate.HasValue ? r.NextFollowUpDate.Value.ToString("dd-MMM-yyyy HH:mm") : "N/A";
                sheet.Cell(rowIdx, 20).Value = r.FollowUpStatus;
                sheet.Cell(rowIdx, 21).Value = r.CurrentStage;
                sheet.Cell(rowIdx, 22).Value = r.Status;
                sheet.Cell(rowIdx, 23).Value = r.DaysOpen;
                sheet.Cell(rowIdx, 24).Value = r.AgeingBucket;
                sheet.Cell(rowIdx, 25).Value = r.ExpectedOrderDate.HasValue ? r.ExpectedOrderDate.Value.ToString("dd-MMM-yyyy") : "N/A";
                sheet.Cell(rowIdx, 26).Value = r.OrderDate.HasValue ? r.OrderDate.Value.ToString("dd-MMM-yyyy") : "N/A";
                sheet.Cell(rowIdx, 27).Value = r.OrderValue;
                sheet.Cell(rowIdx, 28).Value = r.LostReason;

                rowIdx++;
            }

            var usedRange = sheet.Range(1, 1, Math.Max(1, rowIdx - 1), headers.Length);
            usedRange.SetAutoFilter();
            sheet.SheetView.FreezeRows(1);
            sheet.Columns(1, headers.Length).AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static IQueryable<LeadTrackerRow> ApplyFilters(IQueryable<LeadTrackerRow> query, LeadTrackerQueryParams filters)
        {
            if (!string.IsNullOrWhiteSpace(filters.SalesExecutive))
            {
                var exec = filters.SalesExecutive.Trim().ToLower();
                query = query.Where(r => r.SalesExecutive.ToLower().Contains(exec));
            }

            if (!string.IsNullOrWhiteSpace(filters.Status))
            {
                var st = filters.Status.Trim().ToLower();
                query = query.Where(r => r.Status.ToLower() == st);
            }

            if (!string.IsNullOrWhiteSpace(filters.Stage))
            {
                var stg = filters.Stage.Trim().ToLower();
                query = query.Where(r => r.CurrentStage.ToLower() == stg);
            }

            if (!string.IsNullOrWhiteSpace(filters.AgeingBucket))
            {
                var bucket = filters.AgeingBucket.Trim().ToLower();
                switch (bucket)
                {
                    case "0-2 days":
                    case "0-2":
                        query = query.Where(r => r.DaysOpen <= 2 || r.AgeingBucket.ToLower() == "0-2 days");
                        break;
                    case "3-7 days":
                    case "3-7":
                        query = query.Where(r => (r.DaysOpen >= 3 && r.DaysOpen <= 7) || r.AgeingBucket.ToLower() == "3-7 days");
                        break;
                    case "0-7 days":
                    case "0-7":
                        query = query.Where(r => r.DaysOpen <= 7);
                        break;
                    case "8-14 days":
                    case "8-14":
                        query = query.Where(r => (r.DaysOpen >= 8 && r.DaysOpen <= 14) || r.AgeingBucket.ToLower() == "8-14 days");
                        break;
                    case "8-15 days":
                    case "8-15":
                        query = query.Where(r => r.DaysOpen >= 8 && r.DaysOpen <= 15);
                        break;
                    case "15+ days":
                    case "15+":
                        query = query.Where(r => r.DaysOpen >= 15 || r.AgeingBucket.ToLower() == "15+ days");
                        break;
                    case "16-30 days":
                        query = query.Where(r => r.DaysOpen >= 16 && r.DaysOpen <= 30);
                        break;
                    case "31-60 days":
                        query = query.Where(r => r.DaysOpen >= 31 && r.DaysOpen <= 60);
                        break;
                    case "60+ days":
                        query = query.Where(r => r.DaysOpen >= 60);
                        break;
                    default:
                        query = query.Where(r => r.AgeingBucket.ToLower() == bucket);
                        break;
                }
            }

            if (!string.IsNullOrWhiteSpace(filters.FollowUpStatus))
            {
                var due = filters.FollowUpStatus.Trim().ToLower();
                query = query.Where(r => r.FollowupDueStatus.ToLower() == due);
            }

            if (!string.IsNullOrWhiteSpace(filters.Search))
            {
                var term = filters.Search.Trim().ToLower();
                query = query.Where(r =>
                    r.LeadIdFormatted.ToLower().Contains(term) ||
                    r.CustomerCompanyName.ToLower().Contains(term) ||
                    r.ContactPerson.ToLower().Contains(term) ||
                    r.Mobile.ToLower().Contains(term) ||
                    r.SalesExecutive.ToLower().Contains(term) ||
                    r.RequirementProduct.ToLower().Contains(term));
            }

            if (filters.StartDate.HasValue)
            {
                var startUtc = DateTime.SpecifyKind(filters.StartDate.Value.Date, DateTimeKind.Utc);
                query = query.Where(r => r.LeadDate >= startUtc);
            }

            if (filters.EndDate.HasValue)
            {
                var endUtc = DateTime.SpecifyKind(filters.EndDate.Value.Date, DateTimeKind.Utc).AddDays(1).AddTicks(-1);
                query = query.Where(r => r.LeadDate <= endUtc);
            }

            return query;
        }

        private static IQueryable<LeadTrackerRow> ApplySorting(IQueryable<LeadTrackerRow> query, string? sortBy, string? dir)
        {
            bool isDesc = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase);

            return (sortBy?.ToLower()) switch
            {
                "leadidnum" or "leadid" => isDesc ? query.OrderByDescending(r => r.LeadIdNum) : query.OrderBy(r => r.LeadIdNum),
                "customername" or "customer" => isDesc ? query.OrderByDescending(r => r.CustomerCompanyName) : query.OrderBy(r => r.CustomerCompanyName),
                "salesexecutive" => isDesc ? query.OrderByDescending(r => r.SalesExecutive) : query.OrderBy(r => r.SalesExecutive),
                "estimatedleadvalue" or "estimatedvalue" => isDesc ? query.OrderByDescending(r => r.EstimatedLeadValue) : query.OrderBy(r => r.EstimatedLeadValue),
                "quotationvalue" => isDesc ? query.OrderByDescending(r => r.QuotationValue) : query.OrderBy(r => r.QuotationValue),
                "ordervalue" => isDesc ? query.OrderByDescending(r => r.OrderValue) : query.OrderBy(r => r.OrderValue),
                "daysopen" => isDesc ? query.OrderByDescending(r => r.DaysOpen) : query.OrderBy(r => r.DaysOpen),
                "leaddate" => isDesc ? query.OrderByDescending(r => r.LeadDate) : query.OrderBy(r => r.LeadDate),
                _ => isDesc ? query.OrderByDescending(r => r.LeadIdNum) : query.OrderBy(r => r.LeadIdNum),
            };
        }

        private static LeadTrackerRowDto MapToDto(LeadTrackerRow r) => new()
        {
            LeadIdNum = r.LeadIdNum,
            LeadId = r.LeadIdFormatted,
            LeadDate = r.LeadDate,
            MonthFormatted = r.MonthLabel,
            SalesExecutive = r.SalesExecutive,
            SalesExecutiveUserId = r.SalesExecutiveUserId,
            LeadSource = r.LeadSource,
            CustomerName = r.CustomerCompanyName,
            ContactPerson = r.ContactPerson,
            Mobile = r.Mobile,
            RequirementProduct = r.RequirementProduct,
            EstimatedLeadValue = r.EstimatedLeadValue,
            FirstContactDate = r.FirstContactDate,
            Contacted = r.IsContacted ? "Yes" : "No",
            Qualified = r.IsQualified ? "Yes" : "No",
            MeetingDate = r.MeetingDate,
            MeetingDone = r.IsMeetingDone ? "Yes" : "No",
            QuotationDate = r.QuotationDate,
            QuotationValue = r.QuotationValue,
            LastFollowUpDate = r.LastFollowupDate,
            NextFollowUpDate = r.NextFollowupDate,
            FollowUpStatus = string.IsNullOrWhiteSpace(r.FollowupDueStatus) ? "No Schedule" : FormatFollowUpStatus(r.FollowupDueStatus),
            CurrentStage = r.CurrentStage,
            Status = r.Status,
            DaysOpen = r.DaysOpen,
            AgeingBucket = r.AgeingBucket,
            ExpectedOrderDate = r.ExpectedOrderDate,
            OrderDate = r.OrderDate,
            OrderValue = r.OrderValue,
            LostReason = string.IsNullOrWhiteSpace(r.LostReason) ? "N/A" : r.LostReason
        };

        private static string FormatFollowUpStatus(string raw)
        {
            switch (raw.ToUpper())
            {
                case "OVERDUE": return "Overdue";
                case "DUE TODAY": case "TODAY": return "Today";
                case "UPCOMING": return "Upcoming";
                case "NO SCHEDULE": default: return "No Schedule";
            }
        }
    }
}
