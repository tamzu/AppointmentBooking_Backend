using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AppointmentBooking.Core.DTOs.Services
{
    public class CreateServiceRequest
    {
        [Required]
        [MaxLength(30)]
        public string ServiceCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string ServiceName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Range(5, 480)]
        public int DurationMinutes { get; set; } = 30;

        public bool IsActive { get; set; } = true;
    }
}
