using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Core.Entities
{
    public class CustomerIdentityVerification
    {
        public long CustomerIdentityVerificationId { get; set; }

        public long CustomerIdentityDocumentId { get; set; }

        public int IdentityVerificationStatusId { get; set; }

        // MANUAL, API, BIOMETRIC, etc.
        public string VerificationMethod { get; set; } = string.Empty;

        // Reference supplied by an external verification provider.
        public string? ProviderReference { get; set; }

        // Date and time verification was completed.
        public DateTime? VerifiedAt { get; set; }

        // Reason for rejection or additional review.
        public string? FailureReason { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public CustomerIdentityDocument CustomerIdentityDocument { get; set; } = null!;

        public IdentityVerificationStatus IdentityVerificationStatus { get; set; } = null!;
    }
}
