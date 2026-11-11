using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Core.Interfaces.Notifications
{
    public interface IEmailNotificationSender
    {
        Task<string?> SendAsync(
            string recipient,
            string subject,
            string message,
            CancellationToken cancellationToken = default);
    }
}
