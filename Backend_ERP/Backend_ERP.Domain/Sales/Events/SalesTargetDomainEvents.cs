using System;
using ERP.Domain.Common.Events;
using ERP.Domain.Sales;

namespace ERP.Domain.Sales.Events
{
    public class SalesTargetRealizationCreatedDomainEvent : INotification
    {
        public SalesTargetRealization Realization { get; }

        public SalesTargetRealizationCreatedDomainEvent(SalesTargetRealization realization)
        {
            Realization = realization;
        }
    }

    public class SalesTargetRevisedDomainEvent : INotification
    {
        public SalesTarget OldTarget { get; }
        public SalesTarget NewTarget { get; }
        public string Reason { get; }

        public SalesTargetRevisedDomainEvent(SalesTarget oldTarget, SalesTarget newTarget, string reason)
        {
            OldTarget = oldTarget;
            NewTarget = newTarget;
            Reason = reason;
        }
    }

    public class SalesTargetApprovedDomainEvent : INotification
    {
        public SalesTarget Target { get; }
        public string ApproverUserId { get; }

        public SalesTargetApprovedDomainEvent(SalesTarget target, string approverUserId)
        {
            Target = target;
            ApproverUserId = approverUserId;
        }
    }

    public class AdvancePaymentClearedDomainEvent : INotification
    {
        public int AdvancePaymentId { get; }

        public AdvancePaymentClearedDomainEvent(int advancePaymentId)
        {
            AdvancePaymentId = advancePaymentId;
        }
    }

    public class AdvancePaymentRefundedDomainEvent : INotification
    {
        public int AdvancePaymentId { get; }
        public string Reason { get; }

        public AdvancePaymentRefundedDomainEvent(int advancePaymentId, string reason)
        {
            AdvancePaymentId = advancePaymentId;
            Reason = reason;
        }
    }

    public class SalesInvoiceDispatchedDomainEvent : INotification
    {
        public int InvoiceId { get; }

        public SalesInvoiceDispatchedDomainEvent(int invoiceId)
        {
            InvoiceId = invoiceId;
        }
    }
}
