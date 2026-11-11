namespace AppointmentBooking.API.Security
{
    public class IdentityDocumentSecurityOptions
    {
        public const string SectionName = "IdentityDocumentSecurity";

        public string EncryptionKey { get; set; } = string.Empty;

        public string HashKey { get; set; } = string.Empty;

        public short EncryptionKeyVersion { get; set; } = 1;
    }
}
