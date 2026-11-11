using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Core.Entities
{
    /// <summary>
    /// implement branch operating hours.
    /// </summary>
    public class BranchOperatingHour
    {
        public int OperatingHourId { get; set; }

        public int BranchId { get; set; }

        // 0 = Sunday, 6 = Saturday
        public byte DayOfWeek { get; set; }

        public TimeOnly? OpeningTime { get; set; }
        public TimeOnly? ClosingTime { get; set; }

        public bool IsClosed { get; set; }

        public Branch Branch { get; set; } = null!;
    }
}
