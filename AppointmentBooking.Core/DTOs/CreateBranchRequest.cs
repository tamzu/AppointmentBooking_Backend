using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AppointmentBooking.Core.DTOs
{
    public class CreateBranchRequest
    {
        [Required]
        [MaxLength(20)]
        public string BranchCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string BranchName { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string AddressLine1 { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? AddressLine2 { get; set; }

        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Province { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? PostalCode { get; set; }

        [Required]
        [StringLength(2, MinimumLength = 2)]
        public string CountryCode { get; set; } = "ZA";

        [MaxLength(30)]
        public string? PhoneNumber { get; set; }

        [EmailAddress]
        [MaxLength(150)]
        public string? Email { get; set; }

        [Required]
        [MaxLength(100)]
        public string TimeZoneId { get; set; } =
            "South Africa Standard Time";

        public bool IsActive { get; set; } = true;
    }
}
