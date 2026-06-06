using CRM.DTO;
using CRM.models;
using CRM.Services;

namespace CRM.Helpers
{
    /// <summary>
    /// Lead/deal ownership on create and update:
    /// - assign: caller may set any owner
    /// - self_assign: manual create assigns to the logged-in user; owner cannot be changed later
    /// </summary>
    public static class RecordOwnershipEnforcement
    {
    /// Users without assign always self-own on create; assign users keep DTO owner when set.
    public static async Task EnforceLeadOwnerOnCreateAsync(IRbacService rbac, int userId, Lead lead)
    {
        if (await rbac.HasPermissionAsync(userId, "leads.assign") && lead.LeadOwnerId is > 0)
        {
            return;
        }

        lead.LeadOwnerId = userId;
    }

        public static async Task EnforceLeadOwnerOnUpdateAsync(
            IRbacService rbac,
            int userId,
            LeadUpsertDto dto,
            Lead existing)
        {
            if (await rbac.HasPermissionAsync(userId, "leads.assign"))
            {
                return;
            }

            dto.LeadOwnerId = existing.LeadOwnerId;
        }

        public static async Task EnforceDealOwnerOnCreateAsync(IRbacService rbac, int userId, Deal deal)
        {
            var hasOwner = (deal.DealOwnerId is > 0) || (deal.AssignedToUserId is > 0);
            if (await rbac.HasPermissionAsync(userId, "deals.assign") && hasOwner)
            {
                return;
            }

            deal.DealOwnerId = userId;
            deal.AssignedToUserId = userId;
        }

        public static async Task EnforceDealOwnerOnUpdateAsync(
            IRbacService rbac,
            int userId,
            DealUpsertDto dto,
            Deal existing)
        {
            if (await rbac.HasPermissionAsync(userId, "deals.assign"))
            {
                return;
            }

            dto.DealOwnerId = existing.DealOwnerId;
            dto.AssignedToUserId = existing.AssignedToUserId;
        }
    }
}
