namespace AppointmentBooking.Core.Entities
{
    public class Appointment
    {
        public long AppointmentId { get; set; }

        public Guid AppointmentReference { get; set; }

        public int CustomerId { get; set; }
        public long SlotId { get; set; }

        public int AppointmentStatusId { get; set; }

        public DateTime BookingDate { get; set; }

        public string? CustomerNotes { get; set; }
        public string? CancellationReason { get; set; }

        public DateTime? CancelledAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public byte[] RowVersion { get; set; } = [];

        public Customer Customer { get; set; } = null!;
        public AppointmentSlot Slot { get; set; } = null!;

        public AppointmentStatus Status { get; set; } = null!;

        public ICollection<AppointmentNotification> Notifications { get; set; }
            = new List<AppointmentNotification>();

        public ICollection<AppointmentHistory> History { get; set; }
            = new List<AppointmentHistory>();
        public ICollection<AppointmentAccessCode> AccessCodes { get; set; }
    = new List<AppointmentAccessCode>();
        public ICollection<AppointmentAccessSession> AccessSessions { get; set; }
    = new List<AppointmentAccessSession>();
    }
}
