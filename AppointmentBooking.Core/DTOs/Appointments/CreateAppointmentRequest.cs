using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AppointmentBooking.Core.DTOs.Appointments
{
    public class CreateAppointmentRequest
    {
        [Range(1, int.MaxValue)]
        public int CustomerId { get; set; }

        [Range(1, long.MaxValue)]
        public long SlotId { get; set; }

        [MaxLength(500)]
        public string? CustomerNotes { get; set; }
    }
}
