using System;
using ERP.Domain.Common.Events;

namespace ERP.Domain.Procurement.Events
{
    public class VendorCreatedDomainEvent : INotification
    {
        public int VendorId { get; }
        public string VendorCode { get; }
        public string LegalName { get; }
        public string TenantId { get; }

        public VendorCreatedDomainEvent(int vendorId, string vendorCode, string legalName, string tenantId)
        {
            VendorId = vendorId;
            VendorCode = vendorCode;
            LegalName = legalName;
            TenantId = tenantId;
        }
    }

    public class VendorKycApprovedDomainEvent : INotification
    {
        public int VendorId { get; }
        public string VendorCode { get; }
        public string ApprovalToken { get; }
        public string ApprovedByUserId { get; }
        public string TenantId { get; }

        public VendorKycApprovedDomainEvent(int vendorId, string vendorCode, string approvalToken, string approvedByUserId, string tenantId)
        {
            VendorId = vendorId;
            VendorCode = vendorCode;
            ApprovalToken = approvalToken;
            ApprovedByUserId = approvedByUserId;
            TenantId = tenantId;
        }
    }

    public class VendorDisqualifiedDomainEvent : INotification
    {
        public int VendorId { get; }
        public string VendorCode { get; }
        public string Reason { get; }
        public decimal CurrentScore { get; }
        public string TenantId { get; }

        public VendorDisqualifiedDomainEvent(int vendorId, string vendorCode, string reason, decimal currentScore, string tenantId)
        {
            VendorId = vendorId;
            VendorCode = vendorCode;
            Reason = reason;
            CurrentScore = currentScore;
            TenantId = tenantId;
        }
    }

    public class VendorRatingUpdatedDomainEvent : INotification
    {
        public int VendorId { get; }
        public string VendorCode { get; }
        public decimal PreviousScore { get; }
        public decimal NewScore { get; }
        public string PeriodKey { get; }
        public string TenantId { get; }

        public VendorRatingUpdatedDomainEvent(int vendorId, string vendorCode, decimal previousScore, decimal newScore, string periodKey, string tenantId)
        {
            VendorId = vendorId;
            VendorCode = vendorCode;
            PreviousScore = previousScore;
            NewScore = newScore;
            PeriodKey = periodKey;
            TenantId = tenantId;
        }
    }

    public class VendorStatusChangedDomainEvent : INotification
    {
        public int VendorId { get; }
        public string VendorCode { get; }
        public VendorStatus PreviousStatus { get; }
        public VendorStatus NewStatus { get; }
        public string Remarks { get; }
        public string ActionedByUserId { get; }
        public string TenantId { get; }

        public VendorStatusChangedDomainEvent(int vendorId, string vendorCode, VendorStatus previousStatus, VendorStatus newStatus, string remarks, string actionedByUserId, string tenantId)
        {
            VendorId = vendorId;
            VendorCode = vendorCode;
            PreviousStatus = previousStatus;
            NewStatus = newStatus;
            Remarks = remarks;
            ActionedByUserId = actionedByUserId;
            TenantId = tenantId;
        }
    }

    public class VendorCreditLimitBreachedDomainEvent : INotification
    {
        public int VendorId { get; }
        public string VendorCode { get; }
        public decimal CreditLimit { get; }
        public decimal TotalOpenCommitment { get; }
        public decimal AttemptedPoAmount { get; }
        public string TenantId { get; }

        public VendorCreditLimitBreachedDomainEvent(int vendorId, string vendorCode, decimal creditLimit, decimal totalOpenCommitment, decimal attemptedPoAmount, string tenantId)
        {
            VendorId = vendorId;
            VendorCode = vendorCode;
            CreditLimit = creditLimit;
            TotalOpenCommitment = totalOpenCommitment;
            AttemptedPoAmount = attemptedPoAmount;
            TenantId = tenantId;
        }
    }
}

