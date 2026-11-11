using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Core.DTOs.Appointments
{
    public class AppointmentResponse
    {
        public long AppointmentId { get; set; }

        public Guid AppointmentReference { get; set; }

        public int CustomerId { get; set; }

        public long SlotId { get; set; }

        public int BranchId { get; set; }

        public int ServiceId { get; set; }

        public DateTime StartDateTime { get; set; }

        public DateTime EndDateTime { get; set; }

        public string StatusCode { get; set; } = string.Empty;

        public string StatusName { get; set; } = string.Empty;

        public DateTime BookingDate { get; set; }

        public string? CustomerNotes { get; set; }
    }
}
