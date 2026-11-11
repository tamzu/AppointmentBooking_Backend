using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Core.Entities
{
    public class AppointmentNotificationAttempt
    {
        public long NotificationAttemptId { get; set; }

        public long NotificationId { get; set; }

        public int AttemptNumber { get; set; }

        public string Status { get; set; } = "PROCESSING";

        public DateTime StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public string? ErrorMessage { get; set; }

        public string? ProviderReference { get; set; }

        public AppointmentNotification Notification { get; set; } = null!;
    }
}
