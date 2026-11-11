using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Core.Entities
{
    public class Branch
    {
        public int BranchId { get; set; }

        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;

        public string AddressLine1 { get; set; } = string.Empty;
        public string? AddressLine2 { get; set; }

        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string? PostalCode { get; set; }

        public string CountryCode { get; set; } = "ZA";

        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }

        public string TimeZoneId { get; set; }
            = "South Africa Standard Time";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public ICollection<BranchService> BranchServices { get; set; }
            = new List<BranchService>();

        public ICollection<BranchOperatingHour> OperatingHours { get; set; }
            = new List<BranchOperatingHour>();

        public ICollection<BranchClosure> Closures { get; set; }
            = new List<BranchClosure>();

        public ICollection<AppointmentSlot> AppointmentSlots { get; set; }
            = new List<AppointmentSlot>();
    }
}
