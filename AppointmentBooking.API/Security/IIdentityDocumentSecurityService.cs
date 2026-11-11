namespace AppointmentBooking.API.Security
{
    public interface IIdentityDocumentSecurityService
    {
        short EncryptionKeyVersion { get; }
        string NormalizeDocumentNumber(
    string documentNumber,
    string documentTypeCode);

        byte[] Encrypt(string documentNumber);

        byte[] ComputeHash(
            string documentNumber,
            string documentTypeCode,
            string issuingCountryCode);

        string MaskDocumentNumber(string documentNumber);
    }
}
