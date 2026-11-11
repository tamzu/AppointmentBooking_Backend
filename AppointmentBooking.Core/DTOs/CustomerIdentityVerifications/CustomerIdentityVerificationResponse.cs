namespace AppointmentBooking.API.DTOs.CustomerIdentityVerifications
{
    public class CustomerIdentityVerificationResponse
    {
        public long CustomerIdentityVerificationId { get; set; }

        public long CustomerIdentityDocumentId { get; set; }

        public string MaskedDocumentNumber { get; set; } = string.Empty;

        public string DocumentTypeCode { get; set; } = string.Empty;

        public string DocumentTypeName { get; set; } = string.Empty;

        public string StatusCode { get; set; } = string.Empty;

        public string StatusName { get; set; } = string.Empty;

        public string VerificationMethod { get; set; } = string.Empty;

        public string? ProviderReference { get; set; }

        public DateTime? VerifiedAt { get; set; }

        public string? FailureReason { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
