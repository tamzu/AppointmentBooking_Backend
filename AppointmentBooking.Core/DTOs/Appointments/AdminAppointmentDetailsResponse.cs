namespace AppointmentBooking.Core.DTOs.Appointments
{
    public class AdminAppointmentDetailsResponse
    {
        public long AppointmentId { get; set; }
        public Guid AppointmentReference { get; set; }

        // Customer
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public string CustomerPhone { get; set; } = string.Empty;

        // Branch
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string BranchAddress { get; set; } = string.Empty;
        public string BranchCity { get; set; } = string.Empty;
        public string BranchProvince { get; set; } = string.Empty;

        // Service
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string? ServiceDescription { get; set; }
        public int ServiceDurationMinutes { get; set; }

        // Appointment
        public long SlotId { get; set; }
        public DateTime AppointmentDateTime { get; set; }
        public DateTime AppointmentEndDateTime { get; set; }
        public DateTime BookingDate { get; set; }
        public string StatusCode { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public string? CustomerNotes { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime? CancelledAt { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}