using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.API.DTOs.CustomerIdentityVerifications
{
    public class CreateCustomerIdentityVerificationRequest
    {
        [Required]
        public long CustomerIdentityDocumentId { get; set; }

        [Required]
        [MaxLength(30)]
        public string VerificationMethod { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string StatusCode { get; set; } = "PENDING";

        [MaxLength(100)]
        public string? ProviderReference { get; set; }

        [MaxLength(500)]
        public string? FailureReason { get; set; }
    }
}
