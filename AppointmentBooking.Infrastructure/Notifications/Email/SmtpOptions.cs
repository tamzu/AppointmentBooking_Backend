using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Infrastructure.Notifications.Email
{
    public class SmtpOptions
    {
        public const string SectionName = "Smtp";

        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;

        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "Bank Appointment Booking";

        public bool UseStartTls { get; set; } = true;
    }
}
