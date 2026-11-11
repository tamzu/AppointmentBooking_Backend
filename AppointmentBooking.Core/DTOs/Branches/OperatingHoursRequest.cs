using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Core.DTOs.Branches
{
    public class OperatingHoursRequest
    {
        [Range(0, 6)]
        public byte DayOfWeek { get; set; }

        public TimeOnly? OpeningTime { get; set; }

        public TimeOnly? ClosingTime { get; set; }

        public bool IsClosed { get; set; }
    }
}
