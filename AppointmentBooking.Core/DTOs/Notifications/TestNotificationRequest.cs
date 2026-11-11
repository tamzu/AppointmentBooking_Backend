using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Core.DTOs.Notifications
{
    public class TestNotificationRequest
    {
        [Required]
        public string Recipient { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Subject { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;
    }
}
