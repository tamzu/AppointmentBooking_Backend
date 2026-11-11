using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AppointmentBooking.Core.DTOs.Appointments
{
    public class CancelAppointmentRequest
    {
        [Required]
        [MaxLength(500)]
        public string CancellationReason { get; set; } = string.Empty;
    }
}
