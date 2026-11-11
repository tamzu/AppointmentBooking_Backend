using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AppointmentBooking.API.Security
{
    public class IdentityDocumentSecurityService : IIdentityDocumentSecurityService
    {
        private const int NonceSize = 12;
        private const int TagSize = 16;
        private const int KeySize = 32;

        private readonly byte[] _encryptionKey;
        private readonly byte[] _hashKey;

        public IdentityDocumentSecurityService(
            IOptions<IdentityDocumentSecurityOptions> options)
        {
            var settings = options.Value;
            EncryptionKeyVersion = settings.EncryptionKeyVersion;

            if (EncryptionKeyVersion <= 0)
            {
                throw new InvalidOperationException(
                    "The encryption key version must be greater than zero.");
            }
            try
            {
                _encryptionKey = Convert.FromBase64String(
                    settings.EncryptionKey);

                _hashKey = Convert.FromBase64String(
                    settings.HashKey);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(
                    "Identity document security keys must be valid Base64 values.",
                    ex);
            }

            if (_encryptionKey.Length != KeySize)
            {
                throw new InvalidOperationException(
                    "The identity document encryption key must be 32 bytes.");
            }

            if (_hashKey.Length != KeySize)
            {
                throw new InvalidOperationException(
                    "The identity document HMAC key must be 32 bytes.");
            }
        }
        public short EncryptionKeyVersion { get; }
        public string NormalizeDocumentNumber(
            string documentNumber,
            string documentTypeCode)
        {
            var normalized = Regex.Replace(
                documentNumber.Trim().ToUpperInvariant(),
                @"[\s-]",
                string.Empty);

            if (documentTypeCode == "RSA_ID")
            {
                if (!Regex.IsMatch(normalized, @"^\d{13}$"))
                {
                    throw new ArgumentException(
                        "A South African ID number must contain exactly 13 digits.");
                }
            }
            else if (documentTypeCode == "PASSPORT")
            {
                if (!Regex.IsMatch(normalized, @"^[A-Z0-9]{5,20}$"))
                {
                    throw new ArgumentException(
                        "The passport number format is invalid.");
                }
            }

            return normalized;
        }

        public byte[] Encrypt(string documentNumber)
        {
            var plaintext = Encoding.UTF8.GetBytes(documentNumber);

            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var tag = new byte[TagSize];
            var ciphertext = new byte[plaintext.Length];

            using (var aes = new AesGcm(_encryptionKey, TagSize))
            {
                aes.Encrypt(nonce, plaintext, ciphertext, tag);
            }

            // Stored payload:
            // [12-byte nonce][16-byte authentication tag][ciphertext]

            var encryptedPayload = new byte[
                NonceSize + TagSize + ciphertext.Length];

            Buffer.BlockCopy(
                nonce, 0,
                encryptedPayload, 0,
                NonceSize);

            Buffer.BlockCopy(
                tag, 0,
                encryptedPayload, NonceSize,
                TagSize);

            Buffer.BlockCopy(
                ciphertext, 0,
                encryptedPayload, NonceSize + TagSize,
                ciphertext.Length);

            CryptographicOperations.ZeroMemory(plaintext);

            return encryptedPayload;
        }

        public byte[] ComputeHash(
            string documentNumber,
            string documentTypeCode,
            string issuingCountryCode)
        {
            var hashInput =
                $"{documentTypeCode}|{issuingCountryCode}|{documentNumber}";

            var inputBytes = Encoding.UTF8.GetBytes(hashInput);

            using var hmac = new HMACSHA256(_hashKey);

            return hmac.ComputeHash(inputBytes);
        }

        public string MaskDocumentNumber(string documentNumber)
        {
            if (documentNumber.Length <= 4)
            {
                return new string('*', documentNumber.Length);
            }

            var visiblePart = documentNumber[^4..];

            return new string('*', documentNumber.Length - 4)
                   + visiblePart;
        }
    }
}
