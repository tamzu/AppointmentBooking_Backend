using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AppointmentBooking.Core.DTOs.BranchClosures
{
    public class UpdateBranchClosureRequest
    {
        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        [MaxLength(250)]
        public string? Reason { get; set; }
    }
}
