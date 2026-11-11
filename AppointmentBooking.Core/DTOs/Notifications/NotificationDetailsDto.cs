using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Core.DTOs.Notifications
{
    public class NotificationDetailsDto
    {
        public long NotificationId { get; set; }

        public long AppointmentId { get; set; }

        public string NotificationType { get; set; } = string.Empty;

        public string Channel { get; set; } = string.Empty;

        public string Recipient { get; set; } = string.Empty;

        public string? Subject { get; set; }

        public string Message { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public int AttemptCount { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastAttemptAt { get; set; }

        public DateTime? ProcessingStartedAt { get; set; }

        public DateTime? NextAttemptAt { get; set; }

        public DateTime? SentAt { get; set; }

        public string? LastError { get; set; }
    }
}
