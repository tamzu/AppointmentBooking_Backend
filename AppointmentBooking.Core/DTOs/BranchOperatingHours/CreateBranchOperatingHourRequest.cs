using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AppointmentBooking.Core.DTOs.BranchOperatingHours
{
    public class CreateBranchOperatingHourRequest
    {
        [Range(1, int.MaxValue)]
        public int BranchId { get; set; }

        // 0 = Sunday, 1 = Monday, ..., 6 = Saturday
        [Range(0, 6)]
        public byte DayOfWeek { get; set; }

        public TimeOnly? OpeningTime { get; set; }

        public TimeOnly? ClosingTime { get; set; }

        public bool IsClosed { get; set; }
    }
}
