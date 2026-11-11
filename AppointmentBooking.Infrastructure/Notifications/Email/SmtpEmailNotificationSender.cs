
using AppointmentBooking.Core.Interfaces.Notifications;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AppointmentBooking.Infrastructure.Notifications.Email
{
    public class SmtpEmailNotificationSender
        : IEmailNotificationSender
    {
        private readonly SmtpOptions _options;
        private readonly ILogger<SmtpEmailNotificationSender> _logger;

        public SmtpEmailNotificationSender(
            IOptions<SmtpOptions> options,
            ILogger<SmtpEmailNotificationSender> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string?> SendAsync(
            string recipient,
            string subject,
            string message,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(recipient))
                throw new ArgumentException(
                    "Email recipient is required.");

            if (string.IsNullOrWhiteSpace(_options.Host))
                throw new InvalidOperationException(
                    "SMTP host is not configured.");

            if (string.IsNullOrWhiteSpace(_options.FromEmail))
                throw new InvalidOperationException(
                    "SMTP sender email is not configured.");

            var email = new MimeMessage();

            email.From.Add(new MailboxAddress(
                _options.FromName,
                _options.FromEmail));

            email.To.Add(MailboxAddress.Parse(recipient));

            email.Subject = subject;

            email.Body = new TextPart("plain")
            {
                Text = message
            };

            using var client = new SmtpClient();

            try
            {
                var secureSocketOption = _options.UseStartTls
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.Auto;

                await client.ConnectAsync(
                    _options.Host,
                    _options.Port,
                    secureSocketOption,
                    cancellationToken);

                if (!string.IsNullOrWhiteSpace(_options.Username))
                {
                    await client.AuthenticateAsync(
                        _options.Username,
                        _options.Password,
                        cancellationToken);
                }

                await client.SendAsync(email, cancellationToken);

                await client.DisconnectAsync(
                    true,
                    cancellationToken);

                _logger.LogInformation(
                    "Email notification accepted by SMTP server.");

                // SMTP provider reference is not currently tracked.
                return null;
            }
            catch
            {
                if (client.IsConnected)
                {
                    await client.DisconnectAsync(
                        false,
                        CancellationToken.None);
                }

                throw;
            }
        }
    }
}
