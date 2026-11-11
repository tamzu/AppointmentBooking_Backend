using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Core.Interfaces.Notifications
{
    public interface ISmsNotificationSender
    {
        Task<string?> SendAsync(
            string recipient,
            string message,
            CancellationToken cancellationToken = default);
    }
}
