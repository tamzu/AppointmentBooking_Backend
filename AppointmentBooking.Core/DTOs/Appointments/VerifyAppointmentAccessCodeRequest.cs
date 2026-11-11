using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AppointmentBooking.Core.DTOs.Appointments
{
    public class VerifyAppointmentAccessCodeRequest
    {
        [Required]
        public Guid AppointmentReference { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^\d{6}$",
            ErrorMessage = "The access code must contain exactly six digits.")]
        public string Code { get; set; } = string.Empty;
    }
}
