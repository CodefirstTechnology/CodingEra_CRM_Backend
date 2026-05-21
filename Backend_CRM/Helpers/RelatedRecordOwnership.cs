using CRM.DATA;
using CRM.DTO;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    /// <summary>
    /// Tasks and notes linked to a lead or deal are owned by that record's assignee (lead owner / deal assignee).
    /// </summary>
    public static class RelatedRecordOwnership
    {
        public static async Task<int?> ResolveLeadOwnerUserIdAsync(TaskDbcontext db, int? leadId)
        {
            if (leadId is not > 0)
            {
                return null;
            }

            return await db.Leads.AsNoTracking()
                .Where(l => l.Id == leadId)
                .Select(l => l.LeadOwnerId)
                .FirstOrDefaultAsync();
        }

        public static async Task<int?> ResolveDealOwnerUserIdAsync(TaskDbcontext db, int? dealId)
        {
            if (dealId is not > 0)
            {
                return null;
            }

            var row = await db.Deals.AsNoTracking()
                .Where(d => d.Id == dealId)
                .Select(d => new { d.AssignedToUserId, d.DealOwnerId })
                .FirstOrDefaultAsync();

            if (row == null)
            {
                return null;
            }

            return row.AssignedToUserId ?? row.DealOwnerId;
        }

        private static async Task<int?> ResolveOwnerUserIdAsync(TaskDbcontext db, int? relatedLeadId, int? relatedDealId)
        {
            if (relatedLeadId is > 0)
            {
                return await ResolveLeadOwnerUserIdAsync(db, relatedLeadId);
            }

            if (relatedDealId is > 0)
            {
                return await ResolveDealOwnerUserIdAsync(db, relatedDealId);
            }

            return null;
        }

        private static async Task<string?> ResolveUserDisplayNameAsync(TaskDbcontext db, int userId)
        {
            var row = await db.Users.AsNoTracking()
                .Where(u => u.Id == userId && u.IsActive)
                .Select(u => new { u.FullName, u.Email })
                .FirstOrDefaultAsync();

            if (row == null)
            {
                return null;
            }

            var name = row.FullName?.Trim();
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            var email = row.Email?.Trim();
            return !string.IsNullOrEmpty(email) ? email : $"User #{userId}";
        }

        public static async Task ApplyTaskAssigneeFromRelatedRecordAsync(TaskDbcontext db, TaskUpsertDto dto)
        {
            var ownerId = await ResolveOwnerUserIdAsync(db, dto.RelatedLeadId, dto.RelatedDealId);
            if (ownerId is not > 0)
            {
                return;
            }

            dto.AssigneeUserId = ownerId;
            var display = await ResolveUserDisplayNameAsync(db, ownerId.Value);
            if (!string.IsNullOrWhiteSpace(display))
            {
                dto.TaskAssignee = display;
            }
        }

        public static async Task ApplyNoteAuthorFromRelatedRecordAsync(TaskDbcontext db, NoteUpsertDto dto)
        {
            var ownerId = await ResolveOwnerUserIdAsync(db, dto.RelatedLeadId, dto.RelatedDealId);
            if (ownerId is not > 0)
            {
                return;
            }

            dto.AuthorId = ownerId;
            var display = await ResolveUserDisplayNameAsync(db, ownerId.Value);
            if (!string.IsNullOrWhiteSpace(display))
            {
                dto.Name = display;
            }
        }
    }
}
