using ERP.Domain.Common.Events;
using ERP.Domain.Sales;

namespace ERP.Domain.Sales.Events
{
    public class SalesOrderConfirmedDomainEvent : INotification
    {
        public SalesOrder SalesOrder { get; }

        public SalesOrderConfirmedDomainEvent(SalesOrder salesOrder)
        {
            SalesOrder = salesOrder;
        }
    }

    public class SalesOrderCancelledDomainEvent : INotification
    {
        public int SalesOrderId { get; }

        public SalesOrderCancelledDomainEvent(int salesOrderId)
        {
            SalesOrderId = salesOrderId;
        }
    }
}
