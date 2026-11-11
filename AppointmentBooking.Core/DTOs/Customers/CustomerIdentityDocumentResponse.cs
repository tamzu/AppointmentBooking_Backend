using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Core.DTOs.Customers
{
    public class CustomerIdentityDocumentResponse
    {
        public long IdentityDocumentId { get; set; }

        public int CustomerId { get; set; }

        public string DocumentTypeCode { get; set; } = string.Empty;

        public string DocumentTypeName { get; set; } = string.Empty;

        public string MaskedDocumentNumber { get; set; } = string.Empty;

        public string IssuingCountryCode { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public bool IsActive { get; set; }
    }
}
