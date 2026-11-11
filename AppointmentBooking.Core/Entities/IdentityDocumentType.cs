namespace AppointmentBooking.Core.Entities
{
    public class IdentityDocumentType
    {
        public int IdentityDocumentTypeId { get; set; }

        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public ICollection<CustomerIdentityDocument>
            Documents
        { get; set; } = new List<CustomerIdentityDocument>();
    }
}
