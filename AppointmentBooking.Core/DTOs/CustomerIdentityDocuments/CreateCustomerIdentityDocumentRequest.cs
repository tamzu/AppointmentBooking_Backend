using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AppointmentBooking.Core.DTOs.CustomerIdentityDocuments
{
    public class CreateCustomerIdentityDocumentRequest
    {
        [Required]
        public int CustomerId { get; set; }

        [Required]
        [MaxLength(20)]
        public string DocumentTypeCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(2)]
        public string IssuingCountryCode { get; set; } = "ZA";

        [Required]
        [MaxLength(30)]
        public string DocumentNumber { get; set; } = string.Empty;
    }
}
