using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using CRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly TaskDbcontext _context;
        private readonly IRbacService _rbac;

        public DashboardController(TaskDbcontext context, IRbacService rbac)
        {
            _context = context;
            _rbac = rbac;
        }

        [HttpGet("stuck-pipeline")]
        public async Task<IActionResult> GetStuckPipeline([FromQuery] int? userId = null)
        {
            var now = DateTime.UtcNow;
            var dealThreshold = now.AddHours(-24);
            var leadThreshold = now.AddHours(-48);

            // 1. Fetch Open Deals
            var dealsQuery = _context.Deals.AsNoTracking()
                .Include(d => d.DealStatus)
                .Include(d => d.AssignedToUser)
                .Include(d => d.DealOwner)
                .Where(d => d.IsActive);

            if (userId.HasValue && userId.Value > 0)
            {
                var uid = userId.Value;
                if (!await _rbac.IsAdminUserAsync(uid))
                {
                    dealsQuery = dealsQuery.Where(d =>
                        d.DealOwnerId == uid ||
                        d.AssignedToUserId == uid ||
                        d.CreatedBy == uid);
                }
            }

            var allDeals = await dealsQuery.ToListAsync();

            // Exclude closed deals
            var openDeals = allDeals.Where(d =>
            {
                var st = d.Status?.Trim() ?? "";
                if (string.Equals(st, "Closed Won", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(st, "Closed Lost", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(st, "Lead Closed - Won", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(st, "Lead Closed - Lost", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                return true;
            }).ToList();

            // Apply latest quotation values if deal amount is zero or null
            await DealAmountHelper.ApplyLatestQuotationAmountsAsync(_context, openDeals);

            var openDealIds = openDeals.Select(d => d.Id).ToList();

            // Fetch latest activity timestamps for these open deals
            var dealActivities = new Dictionary<int, DateTime>();
            if (openDealIds.Count > 0)
            {
                dealActivities = await _context.ActivityLogs.AsNoTracking()
                    .Where(a => a.EntityType == ActivityEntityTypes.Deal && openDealIds.Contains(a.EntityId))
                    .GroupBy(a => a.EntityId)
                    .Select(g => new { DealId = g.Key, MaxActivity = g.Max(a => a.CreatedAt) })
                    .ToDictionaryAsync(x => x.DealId, x => x.MaxActivity);
            }

            // Identify Stuck Deals (> 24 hours inactive)
            var stuckDealsList = new List<StuckDealItemDto>();
            foreach (var deal in openDeals)
            {
                var recordDate = deal.UpdatedAt > DateTime.MinValue
                    ? deal.UpdatedAt
                    : (deal.LastModified > DateTime.MinValue ? deal.LastModified : deal.CreatedAt);

                if (dealActivities.TryGetValue(deal.Id, out var actDate) && actDate > recordDate)
                {
                    recordDate = actDate;
                }

                if (recordDate < dealThreshold)
                {
                    var idleHours = Math.Max(24, (int)Math.Floor((now - recordDate).TotalHours));
                    var idleDurationStr = FormatIdleDuration(idleHours);

                    var dealTitle = !string.IsNullOrWhiteSpace(deal.OrganizationName)
                        ? deal.OrganizationName
                        : (!string.IsNullOrWhiteSpace(deal.FirstName) || !string.IsNullOrWhiteSpace(deal.LastName)
                            ? $"{deal.FirstName} {deal.LastName}".Trim()
                            : $"Deal #{deal.Id}");

                    var contactName = $"{deal.FirstName} {deal.LastName}".Trim();
                    if (string.IsNullOrWhiteSpace(contactName)) contactName = deal.OrganizationName;

                    stuckDealsList.Add(new StuckDealItemDto
                    {
                        DealId = deal.Id,
                        DealTitle = dealTitle,
                        OrganizationName = deal.OrganizationName ?? "",
                        ContactName = contactName ?? "",
                        Stage = !string.IsNullOrWhiteSpace(deal.DealStatus?.Name) ? deal.DealStatus.Name : deal.Status,
                        DealAmount = deal.DealAmount.GetValueOrDefault(0),
                        LastActivityAt = recordDate,
                        IdleHours = idleHours,
                        IdleDurationFormatted = idleDurationStr,
                        OwnerId = deal.DealOwnerId ?? deal.AssignedToUserId ?? deal.CreatedBy,
                        OwnerName = deal.DealOwner?.FullName ?? deal.AssignedToUser?.FullName ?? "Unassigned"
                    });
                }
            }

            stuckDealsList = stuckDealsList.OrderByDescending(d => d.IdleHours).ToList();

            // 2. Fetch Idle Leads (status = 'New' with no interaction for > 48h)
            var leadsQuery = _context.Leads.AsNoTracking()
                .Include(l => l.LeadStatus)
                .Include(l => l.Organization)
                .Include(l => l.LeadOwner)
                .Where(l => l.IsActive);

            if (userId.HasValue && userId.Value > 0)
            {
                var uid = userId.Value;
                if (!await _rbac.IsAdminUserAsync(uid))
                {
                    leadsQuery = leadsQuery.Where(l => l.LeadOwnerId == uid || l.CreatedBy == uid);
                }
            }

            var allLeads = await leadsQuery.ToListAsync();

            var newLeads = allLeads.Where(l =>
            {
                var st = l.LeadStatus?.Name?.Trim() ?? "New";
                return string.Equals(st, "New", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(st, "Open", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(st, "Uncontacted", StringComparison.OrdinalIgnoreCase);
            }).ToList();

            var newLeadIds = newLeads.Select(l => l.Id).ToList();
            var leadActivities = new Dictionary<int, DateTime>();
            if (newLeadIds.Count > 0)
            {
                leadActivities = await _context.ActivityLogs.AsNoTracking()
                    .Where(a => a.EntityType == ActivityEntityTypes.Lead && newLeadIds.Contains(a.EntityId))
                    .GroupBy(a => a.EntityId)
                    .Select(g => new { LeadId = g.Key, MaxActivity = g.Max(a => a.CreatedAt) })
                    .ToDictionaryAsync(x => x.LeadId, x => x.MaxActivity);
            }

            var idleLeadsList = new List<IdleLeadItemDto>();
            foreach (var lead in newLeads)
            {
                DateTime? lastAct = null;
                if (leadActivities.TryGetValue(lead.Id, out var actDate))
                {
                    lastAct = actDate;
                }
                else
                {
                    lastAct = lead.CreatedAt ?? lead.LeadDate ?? lead.UpdatedAt;
                }

                if (lastAct == null || lastAct.Value < leadThreshold)
                {
                    var effectiveDate = lastAct ?? lead.UpdatedAt;
                    var idleHours = Math.Max(48, (int)Math.Floor((now - effectiveDate).TotalHours));
                    var idleDurationStr = FormatIdleDuration(idleHours);

                    var fullName = $"{lead.FirstName} {lead.LastName}".Trim();
                    if (string.IsNullOrWhiteSpace(fullName)) fullName = lead.Organization?.Name ?? $"Lead #{lead.Id}";

                    idleLeadsList.Add(new IdleLeadItemDto
                    {
                        LeadId = lead.Id,
                        LeadName = fullName,
                        OrganizationName = lead.Organization?.Name ?? "",
                        Status = lead.LeadStatus?.Name ?? "New",
                        CreatedAt = lead.CreatedAt ?? lead.LeadDate,
                        LastActivityAt = lastAct,
                        IdleHours = idleHours,
                        IdleDurationFormatted = idleDurationStr,
                        OwnerId = lead.LeadOwnerId ?? lead.CreatedBy,
                        OwnerName = lead.LeadOwner?.FullName ?? "Unassigned",
                        Mobile = lead.Mobile ?? "",
                        Email = lead.Email ?? ""
                    });
                }
            }

            idleLeadsList = idleLeadsList.OrderByDescending(l => l.IdleHours).ToList();

            // 3. Compute Aggregated Summary
            var totalStuckValue = stuckDealsList.Sum(d => d.DealAmount);
            var avgHours = stuckDealsList.Count > 0 ? stuckDealsList.Average(d => d.IdleHours) : 0;
            var avgDays = Math.Round(avgHours / 24.0, 1);
            var avgIdleStr = avgDays >= 1.0 ? $"{avgDays} Days" : $"{Math.Round(avgHours)} Hours";

            var response = new StuckPipelineResponseDto
            {
                Summary = new StuckPipelineSummaryDto
                {
                    StuckValue = totalStuckValue,
                    StuckDealsCount = stuckDealsList.Count,
                    AvgIdleHours = avgHours,
                    AvgIdleTimeFormatted = avgIdleStr,
                    IdleLeadsCount = idleLeadsList.Count
                },
                StuckDeals = stuckDealsList,
                IdleLeads = idleLeadsList
            };

            return Ok(response);
        }

        private static string FormatIdleDuration(int hours)
        {
            if (hours >= 48)
            {
                var days = hours / 24;
                return $"{days}d Inactive";
            }
            return $"{hours}h Inactive";
        }
    }
}
