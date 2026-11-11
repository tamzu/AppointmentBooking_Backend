using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Core.Entities
{
    public class AppointmentHistory
    {
        public long AppointmentHistoryId { get; set; }

        public long AppointmentId { get; set; }

        public int? OldStatusId { get; set; }
        public int NewStatusId { get; set; }

        public DateTime ChangedAt { get; set; }

        public string ChangedBy { get; set; } = "SYSTEM";
        public string? Comments { get; set; }

        public Appointment Appointment { get; set; } = null!;

        public AppointmentStatus? OldStatus { get; set; }
        public AppointmentStatus NewStatus { get; set; } = null!;
    }
}
