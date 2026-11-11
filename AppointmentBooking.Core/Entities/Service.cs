namespace AppointmentBooking.Core.Entities
{
    public class Service
    {
        public int ServiceId { get; set; }

        public string ServiceCode { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int DurationMinutes { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<BranchService> BranchServices { get; set; }
            = new List<BranchService>();

        public ICollection<AppointmentSlot> AppointmentSlots { get; set; }
            = new List<AppointmentSlot>();
    }
}
