using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AppointmentBooking.Core.DTOs.Customers
{
    public class RegisterIdentityDocumentRequest
    {
        [Required]
        [StringLength(30)]
        public string DocumentTypeCode { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string DocumentNumber { get; set; } = string.Empty;

        // ZA for an RSA ID. For a passport, provide its issuing country.
        [Required]
        [StringLength(2, MinimumLength = 2)]
        public string IssuingCountryCode { get; set; } = "ZA";
    }
}
