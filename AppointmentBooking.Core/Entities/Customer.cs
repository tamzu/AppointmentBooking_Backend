namespace AppointmentBooking.Core.Entities
{

    public class Customer
    {
        public int CustomerId { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public string? Email { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<CustomerIdentityDocument>
            IdentityDocuments
        { get; set; } = new List<CustomerIdentityDocument>();

        public ICollection<Appointment>
            Appointments
        { get; set; } = new List<Appointment>();
    }
}
