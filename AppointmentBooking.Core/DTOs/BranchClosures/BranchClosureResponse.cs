using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Core.DTOs.BranchClosures
{
    public class BranchClosureResponse
    {
        public int BranchClosureId { get; set; }

        public int BranchId { get; set; }

        public string BranchName { get; set; } = string.Empty;

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public string? Reason { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
