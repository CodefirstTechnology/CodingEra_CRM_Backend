using System;
using ERP.Domain.Common.Events;

namespace ERP.Domain.Sales.Events
{
    public class CommissionAccruedDomainEvent : INotification
    {
        public int LedgerId { get; set; }
        public int SalesPersonUserId { get; set; }
        public int SalesOrderId { get; set; }
        public decimal AccruedAmount { get; set; }
        public string PayoutStatus { get; set; } = string.Empty;

        public CommissionAccruedDomainEvent(int ledgerId, int salesPersonUserId, int salesOrderId, decimal accruedAmount, string payoutStatus)
        {
            LedgerId = ledgerId;
            SalesPersonUserId = salesPersonUserId;
            SalesOrderId = salesOrderId;
            AccruedAmount = accruedAmount;
            PayoutStatus = payoutStatus;
        }
    }

    public class CommissionClawbackDomainEvent : INotification
    {
        public int LedgerId { get; set; }
        public int SalesPersonUserId { get; set; }
        public int SalesOrderId { get; set; }
        public decimal ClawbackAmount { get; set; }
        public string Reason { get; set; }

        public CommissionClawbackDomainEvent(int ledgerId, int salesPersonUserId, int salesOrderId, decimal clawbackAmount, string reason)
        {
            LedgerId = ledgerId;
            SalesPersonUserId = salesPersonUserId;
            SalesOrderId = salesOrderId;
            ClawbackAmount = clawbackAmount;
            Reason = reason;
        }
    }

    public class CommissionPayoutApprovedDomainEvent : INotification
    {
        public int LedgerId { get; set; }
        public int SalesPersonUserId { get; set; }
        public decimal PayoutAmount { get; set; }
        public string ApproverUserId { get; set; }
        public string ApprovalToken { get; set; }

        public CommissionPayoutApprovedDomainEvent(int ledgerId, int salesPersonUserId, decimal payoutAmount, string approverUserId, string approvalToken)
        {
            LedgerId = ledgerId;
            SalesPersonUserId = salesPersonUserId;
            PayoutAmount = payoutAmount;
            ApproverUserId = approverUserId;
            ApprovalToken = approvalToken;
        }
    }

    public class BadgeAwardedDomainEvent : INotification
    {
        public int ProfileId { get; set; }
        public int SalesPersonUserId { get; set; }
        public string BadgeCode { get; set; }
        public string BadgeTitle { get; set; }

        public BadgeAwardedDomainEvent(int profileId, int salesPersonUserId, string badgeCode, string badgeTitle)
        {
            ProfileId = profileId;
            SalesPersonUserId = salesPersonUserId;
            BadgeCode = badgeCode;
            BadgeTitle = badgeTitle;
        }
    }
}
