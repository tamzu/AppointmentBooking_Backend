using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Core.DTOs.Services
{
    public class BranchServiceResponse
    {
        public int BranchId { get; set; }

        public int ServiceId { get; set; }

        public string ServiceCode { get; set; } = string.Empty;

        public string ServiceName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int DurationMinutes { get; set; }

        public bool IsAvailable { get; set; }
    }
}
