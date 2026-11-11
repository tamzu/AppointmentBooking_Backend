namespace AppointmentBooking.Core.DTOs.AppointmentSlots
{
    public class AvailableAppointmentSlotResponse
    {
        public long SlotId { get; set; }

        public int BranchId { get; set; }

        public string BranchName { get; set; } = string.Empty;

        public int ServiceId { get; set; }

        public string ServiceName { get; set; } = string.Empty;

        public DateTime StartDateTime { get; set; }

        public DateTime EndDateTime { get; set; }

        public int Capacity { get; set; }

        public int BookedCount { get; set; }

        public int AvailableCapacity => Capacity - BookedCount;
    }

}
