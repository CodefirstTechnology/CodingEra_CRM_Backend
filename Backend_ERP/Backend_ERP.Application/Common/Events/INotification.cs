using ERP.Domain.Common.Events;

namespace ERP.Application.Common.Events
{
    public interface INotification : ERP.Domain.Common.Events.INotification
    {
    }

    public interface INotificationHandler<in TNotification> where TNotification : ERP.Domain.Common.Events.INotification
    {
        Task Handle(TNotification notification, CancellationToken ct);
    }
}
