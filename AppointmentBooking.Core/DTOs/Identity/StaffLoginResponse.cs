using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Core.DTOs.Identity
{
    public class StaffLoginResponse
    {
        public bool Success { get; set; }

        public string AccessToken { get; set; } = string.Empty;

        public string TokenType { get; set; } = "Bearer";

        public int ExpiresIn { get; set; }

        public DateTime ExpiresAt { get; set; }

        public string Email { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public IEnumerable<string> Roles { get; set; }
            = Array.Empty<string>();
    }
}
