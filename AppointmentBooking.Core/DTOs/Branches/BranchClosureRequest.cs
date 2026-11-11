using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AppointmentBooking.Core.DTOs.Branches
{
    public class BranchClosureRequest
    {
        [Required]
        public DateOnly ClosureDate { get; set; }

        [Required]
        [MaxLength(250)]
        public string Reason { get; set; } = string.Empty;
    }
}
