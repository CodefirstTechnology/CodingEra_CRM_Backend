using System;

namespace ERP.Application.Procurement
{
    public interface IPurchaseRequisitionApprovalEngine
    {
        int DetermineRequiredTier(decimal totalEstimatedAmount);

        string GenerateApprovalToken(string prNumber, int tier, string approverUserId, DateTime timestamp);

        bool ValidateApprovalToken(string prNumber, int tier, string approverUserId, DateTime timestamp, string token);

        bool CanUserApproveTier(string userRole, int tier);
    }
}

