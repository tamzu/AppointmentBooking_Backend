namespace AppointmentBooking.Core.Entities
{
    public class AppointmentNotification
    {
        public long NotificationId { get; set; }

        public long AppointmentId { get; set; }

        public string NotificationType { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;

        public string Recipient { get; set; } = string.Empty;

        public string? Subject { get; set; }
        public string Message { get; set; } = string.Empty;

        public string Status { get; set; } = "PENDING";

        public int AttemptCount { get; set; } = 0;

        public DateTime? LastAttemptAt { get; set; }
        public DateTime? ProcessingStartedAt { get; set; }

        public string? LastError { get; set; }

        public DateTime? SentAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? NextAttemptAt { get; set; }

        public Appointment Appointment { get; set; } = null!;
        public ICollection<AppointmentNotificationAttempt> Attempts { get; set; }
    = new List<AppointmentNotificationAttempt>();
    }
}