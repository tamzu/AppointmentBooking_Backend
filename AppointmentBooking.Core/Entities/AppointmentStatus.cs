namespace AppointmentBooking.Core.Entities
{
    public class AppointmentStatus
    {
        public int AppointmentStatusId { get; set; }

        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public ICollection<Appointment> Appointments { get; set; }
            = new List<Appointment>();
    }
}
