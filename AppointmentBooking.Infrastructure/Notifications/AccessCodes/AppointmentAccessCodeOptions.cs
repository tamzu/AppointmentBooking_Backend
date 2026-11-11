
namespace AppointmentBooking.Infrastructure.Notifications.AccessCodes
{
    public class AppointmentAccessCodeOptions
    {
        public const string SectionName = "AppointmentAccessCode";

        public string HmacSecret { get; set; } = string.Empty;

        public int ExpiryMinutes { get; set; } = 5;

        public int MaxAttempts { get; set; } = 5;
    }
}
