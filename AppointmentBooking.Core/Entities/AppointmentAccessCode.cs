
namespace AppointmentBooking.Core.Entities
{
    public class AppointmentAccessCode
    {
        public long AppointmentAccessCodeId { get; set; }

        public long AppointmentId { get; set; }

        // HMAC-SHA256 hash of the six-digit code.
        public string CodeHash { get; set; } = string.Empty;

        public string Channel { get; set; } = "EMAIL";

        public DateTime ExpiresAt { get; set; }

        public int AttemptCount { get; set; }

        public DateTime? UsedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public Appointment Appointment { get; set; } = null!;
    }
}
