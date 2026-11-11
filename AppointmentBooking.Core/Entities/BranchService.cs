namespace AppointmentBooking.Core.Entities
{
    /// <summary>
    /// Implements.
    /// </summary>
    public class BranchService
    {
        public int BranchServiceId { get; set; }

        public int BranchId { get; set; }
        public int ServiceId { get; set; }

        public bool IsAvailable { get; set; } = true;

        public Branch Branch { get; set; } = null!;
        public Service Service { get; set; } = null!;
    }
}
