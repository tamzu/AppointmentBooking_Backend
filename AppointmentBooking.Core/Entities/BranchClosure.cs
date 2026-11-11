namespace AppointmentBooking.Core.Entities
{
    public class BranchClosure
    {
        public int BranchClosureId { get; set; }

        public int BranchId { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public string? Reason { get; set; }

        public DateTime CreatedAt { get; set; }

        public Branch Branch { get; set; } = null!;
    }
}
