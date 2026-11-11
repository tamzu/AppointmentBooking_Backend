using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Core.DTOs.BranchOperatingHours
{
    public class BranchOperatingHourResponse
    {
        public int OperatingHourId { get; set; }

        public int BranchId { get; set; }

        public string BranchName { get; set; } = string.Empty;

        public byte DayOfWeek { get; set; }

        public string DayName { get; set; } = string.Empty;

        public TimeOnly? OpeningTime { get; set; }

        public TimeOnly? ClosingTime { get; set; }

        public bool IsClosed { get; set; }
    }
}
