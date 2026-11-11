using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Core.Entities
{
    public class IdentityVerificationStatus
    {
        public int IdentityVerificationStatusId { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
