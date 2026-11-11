using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Core.DTOs.BranchServices
{
    public class BranchServiceResponse
    {
        public int BranchServiceId { get; set; }

        public int BranchId { get; set; }
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;

        public int ServiceId { get; set; }
        public string ServiceCode { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;

        public int DurationMinutes { get; set; }

        public bool IsAvailable { get; set; }
    }
}
