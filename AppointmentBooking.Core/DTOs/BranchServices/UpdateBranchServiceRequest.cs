using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Core.DTOs.BranchServices
{
    public class UpdateBranchServiceRequest
    {
        public bool IsAvailable { get; set; } = true;
    }
}
