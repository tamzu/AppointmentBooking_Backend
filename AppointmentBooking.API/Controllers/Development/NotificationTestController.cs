using AppointmentBooking.Core.DTOs.Notifications;
using AppointmentBooking.Core.Interfaces.Notifications;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.API.Controllers.Development
{
    [Route("api/development/notifications")]
    [ApiController]
    public class NotificationTestController : ControllerBase
    {
        private readonly IEmailNotificationSender _emailSender;
        private readonly ISmsNotificationSender _smsSender;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<NotificationTestController> _logger;

        public NotificationTestController(
            IEmailNotificationSender emailSender,
            ISmsNotificationSender smsSender,
            IWebHostEnvironment environment,
            ILogger<NotificationTestController> logger)
        {
            _emailSender = emailSender;
            _smsSender = smsSender;
            _environment = environment;
            _logger = logger;
        }

        [HttpPost("email")]
        public async Task<IActionResult> SendTestEmail(
            [FromBody] TestNotificationRequest request,
            CancellationToken cancellationToken)
        {
            if (!_environment.IsDevelopment())
                return NotFound();

            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute()
                .IsValid(request.Recipient))
            {
                return BadRequest(new
                {
                    message = "A valid email address is required."
                });
            }

            try
            {
                await _emailSender.SendAsync(
                    request.Recipient,
                    request.Subject ?? "Appointment Booking - Test Email",
                    request.Message,
                    cancellationToken);

                return Accepted(new
                {
                    message = "Email accepted by the SMTP server.",
                    recipient = request.Recipient
                });
            }
            catch (Exception ex)
                when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Test email delivery failed.");

                return StatusCode(502, new
                {
                    message = "Email delivery failed. Check the application logs."
                });
            }
        }

        [HttpPost("sms")]
        public async Task<IActionResult> SendTestSms(
            [FromBody] TestNotificationRequest request,
            CancellationToken cancellationToken)
        {
            if (!_environment.IsDevelopment())
                return NotFound();

            try
            {
                await _smsSender.SendAsync(
                    request.Recipient,
                    request.Message,
                    cancellationToken);

                return Accepted(new
                {
                    message = "SMS accepted by Clickatell for processing.",
                    recipient = request.Recipient
                });
            }
            catch (Exception ex)
                when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Test SMS delivery failed.");

                return StatusCode(502, new
                {
                    message = "SMS delivery failed. Check the application logs."
                });
            }
        }
    }
}
