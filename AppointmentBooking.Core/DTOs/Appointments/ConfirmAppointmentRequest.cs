using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AppointmentBooking.Core.DTOs.Appointments
{
    public class ConfirmAppointmentRequest
    {
        [MaxLength(500)]
        public string? Comments { get; set; }
    }
}
