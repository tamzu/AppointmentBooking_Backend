namespace AppointmentBooking.Core.Entities
{
    public class CustomerIdentityDocument
    {
        public long IdentityDocumentId { get; set; }

        public int CustomerId { get; set; }
        public int IdentityDocumentTypeId { get; set; }

        public byte[] DocumentNumberEncrypted { get; set; } = [];
        public byte[]? DocumentNumberHash { get; set; }
        // Example: **********8001
        public string MaskedDocumentNumber { get; set; } = string.Empty;

        // ZA for South African ID documents.
        // For passports, this represents the issuing country.
        public string IssuingCountryCode { get; set; } = "ZA";

        // Identifies the encryption key version.
        public short EncryptionKeyVersion { get; set; } = 1;
        public string CountryOfIssue { get; set; } = "ZA";

        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        public Customer Customer { get; set; } = null!;

        public IdentityDocumentType IdentityDocumentType { get; set; } = null!;

        public ICollection<CustomerIdentityVerification>
            Verifications
        { get; set; } = new List<CustomerIdentityVerification>();
    }
}
