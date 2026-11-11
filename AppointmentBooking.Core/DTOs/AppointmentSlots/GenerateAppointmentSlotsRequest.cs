using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AppointmentBooking.Core.DTOs.AppointmentSlots
{
    public class GenerateAppointmentSlotsRequest
    {
        [Range(1, int.MaxValue)]
        public int BranchId { get; set; }

        [Range(1, int.MaxValue)]
        public int ServiceId { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        [Range(1, 100)]
        public int Capacity { get; set; } = 1;
    }
}
