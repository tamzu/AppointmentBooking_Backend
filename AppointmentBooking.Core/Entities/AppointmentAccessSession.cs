namespace AppointmentBooking.Core.Entities
{
    public class AppointmentAccessSession
    {
        public long AppointmentAccessSessionId { get; set; }

        public long AppointmentId { get; set; }

        // SHA-256 hash of the access token. Never store the raw token.
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        public Appointment Appointment { get; set; } = null!;
    }
}