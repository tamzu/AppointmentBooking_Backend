using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Core.Interfaces.Notifications
{
    public interface INotificationProcessingService
    {
        Task<int> ProcessPendingNotificationsAsync(
            CancellationToken cancellationToken = default);
    }
}
