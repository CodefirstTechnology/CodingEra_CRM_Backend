using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement
{
    public interface IPurchaseOrderApprovalEngine
    {
        Task<(PurchaseOrderApprovalTier RequiredTier, bool IsException, List<string> ExceptionReasons)> EvaluateDOARequirementsAsync(
            PurchaseOrder po, 
            decimal? vendorRatingScore = null, 
            bool isUnbudgetedCapEx = false,
            bool isNonAvlEmergencyOrder = false,
            CancellationToken cancellationToken = default);

        string GenerateHmacSignature(int tenantId, string poNumber, int revisionNumber, decimal totalAmount, string approverUserId, DateTime actionedAt);

        bool VerifyHmacSignature(string token, int tenantId, string poNumber, int revisionNumber, decimal totalAmount, string approverUserId, DateTime actionedAt);

        Task<POApprovalSignOffResultDto> ProcessApprovalActionAsync(int poId, POApprovalSignOffRequestDto request, CancellationToken cancellationToken = default);
    }
}

