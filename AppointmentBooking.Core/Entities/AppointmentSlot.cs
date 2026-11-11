using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Core.Entities
{
    public class AppointmentSlot
    {
        public long SlotId { get; set; }

        public int BranchId { get; set; }
        public int ServiceId { get; set; }

        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }

        public int Capacity { get; set; } = 1;
        public int BookedCount { get; set; }

        public bool IsActive { get; set; } = true;

        public byte[] RowVersion { get; set; } = [];

        public Branch Branch { get; set; } = null!;
        public Service Service { get; set; } = null!;

        public ICollection<Appointment> Appointments { get; set; }
            = new List<Appointment>();
    }
}
