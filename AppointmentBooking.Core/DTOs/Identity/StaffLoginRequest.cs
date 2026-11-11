using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Core.DTOs.Identity
{
    public class StaffLoginRequest
    {
        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
