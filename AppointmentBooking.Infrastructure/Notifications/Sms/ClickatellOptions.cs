namespace AppointmentBooking.Infrastructure.Notifications.Sms
{
    public class ClickatellOptions
    {
        public const string SectionName = "Clickatell";

        public string ApiKey { get; set; } = string.Empty;

        public string BaseUrl { get; set; } =
            "https://platform.clickatell.com";

        public string? SenderId { get; set; }
    }
}
