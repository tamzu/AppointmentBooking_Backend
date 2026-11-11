using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Interfaces.Appointments;
using AppointmentBooking.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace AppointmentBooking.Infrastructure.Notifications.AccessCodes
{
    public class AppointmentAccessCodeService
        : IAppointmentAccessCodeService
    {
        private const string AccessCodeProtectorPurpose =
            "AppointmentBooking.AppointmentAccessCode.Notification.v1";

        private const int CodeLength = 6;
        private const int MaxRequests = 3;

        private static readonly TimeSpan RequestWindow =
            TimeSpan.FromMinutes(15);

        private readonly AppointmentBookingDbContext _context;
        private readonly AppointmentAccessCodeOptions _options;
        private readonly IDataProtector _accessCodeProtector;
        private readonly ILogger<AppointmentAccessCodeService> _logger;
        private readonly byte[] _hmacKey;

        public AppointmentAccessCodeService(
            AppointmentBookingDbContext context,
            IOptions<AppointmentAccessCodeOptions> options,
            IDataProtectionProvider dataProtectionProvider,
            ILogger<AppointmentAccessCodeService> logger)
        {
            _context = context;
            _options = options.Value;
            _logger = logger;

            if (string.IsNullOrWhiteSpace(_options.HmacSecret))
            {
                throw new InvalidOperationException(
                    "AppointmentAccessCode:HmacSecret is not configured.");
            }

            try
            {
                _hmacKey = Convert.FromBase64String(_options.HmacSecret);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(
                    "AppointmentAccessCode:HmacSecret must be valid Base64.",
                    ex);
            }

            if (_hmacKey.Length < 32)
            {
                throw new InvalidOperationException(
                    "AppointmentAccessCode:HmacSecret must contain at least 32 bytes.");
            }

            if (_options.ExpiryMinutes <= 0 || _options.MaxAttempts <= 0)
            {
                throw new InvalidOperationException(
                    "Appointment access code expiry and maximum attempts must be greater than zero.");
            }

            _accessCodeProtector = dataProtectionProvider.CreateProtector(
                AccessCodeProtectorPurpose);
        }

        public async Task RequestAccessCodeAsync(
            Guid appointmentReference,
            string email,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                return;

            var normalizedEmail = email.Trim().ToLowerInvariant();

            // Generate the code once so an execution-strategy retry
            // does not create a different code.
            var code = RandomNumberGenerator
                .GetInt32(0, 1_000_000)
                .ToString("D6");

            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                _context.ChangeTracker.Clear();

                await using var transaction =
                    await _context.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);

                var appointment = await _context
                    .Set<Appointment>()
                    .Include(x => x.Customer)
                    .FirstOrDefaultAsync(
                        x =>
                            x.AppointmentReference == appointmentReference &&
                            x.Customer.Email != null &&
                            x.Customer.Email.ToLower() == normalizedEmail,
                        cancellationToken);

                // Do not disclose whether the appointment or email exists.
                if (appointment == null)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return;
                }

                var now = DateTime.UtcNow;
                var requestWindowStart = now - RequestWindow;

                var recentRequestCount = await _context
                    .Set<AppointmentAccessCode>()
                    .CountAsync(
                        x =>
                            x.AppointmentId == appointment.AppointmentId &&
                            x.CreatedAt >= requestWindowStart,
                        cancellationToken);

                // Silently enforce the request limit.
                if (recentRequestCount >= MaxRequests)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return;
                }

                // Invalidate any previously issued, unused codes.
                var previousCodes = await _context
                    .Set<AppointmentAccessCode>()
                    .Where(
                        x =>
                            x.AppointmentId == appointment.AppointmentId &&
                            x.UsedAt == null)
                    .ToListAsync(cancellationToken);

                foreach (var previousCode in previousCodes)
                {
                    previousCode.UsedAt = now;
                }

                var accessCode = new AppointmentAccessCode
                {
                    AppointmentId = appointment.AppointmentId,
                    CodeHash = GenerateCodeHash(
                        appointment.AppointmentId,
                        code),
                    Channel = "EMAIL",
                    ExpiresAt = now.AddMinutes(_options.ExpiryMinutes),
                    AttemptCount = 0,
                    CreatedAt = now
                };

                var message =
                    $"Your appointment access code is: {code}\n\n" +
                    $"This code expires in {_options.ExpiryMinutes} minutes. " +
                    "Do not share it with anyone.";

                // The notification queue must never store the OTP in plaintext.
                var protectedMessage = _accessCodeProtector.Protect(message);

                var notification = new AppointmentNotification
                {
                    AppointmentId = appointment.AppointmentId,
                    NotificationType = "APPOINTMENT_ACCESS_CODE",
                    Channel = "EMAIL",
                    Recipient = appointment.Customer.Email!,
                    Subject = "Your appointment access code",
                    Message = protectedMessage,
                    Status = "PENDING",
                    AttemptCount = 0,
                    CreatedAt = now
                };

                _context.Set<AppointmentAccessCode>().Add(accessCode);
                _context.Set<AppointmentNotification>().Add(notification);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "An appointment access code was queued for appointment {AppointmentId}.",
                    appointment.AppointmentId);
            });
        }

        public async Task<bool> VerifyAccessCodeAsync(
            Guid appointmentReference,
            string email,
            string code,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var normalizedEmail = email.Trim().ToLowerInvariant();

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                _context.ChangeTracker.Clear();

                await using var transaction =
                    await _context.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);

                var now = DateTime.UtcNow;

                var accessCode = await _context
                    .Set<AppointmentAccessCode>()
                    .Include(x => x.Appointment)
                    .ThenInclude(x => x.Customer)
                    .Where(
                        x =>
                            x.Appointment.AppointmentReference ==
                                appointmentReference &&
                            x.Appointment.Customer.Email != null &&
                            x.Appointment.Customer.Email.ToLower() ==
                                normalizedEmail &&
                            x.UsedAt == null &&
                            x.ExpiresAt > now)
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                if (accessCode == null)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return false;
                }

                if (accessCode.AttemptCount >= _options.MaxAttempts)
                {
                    accessCode.UsedAt = now;

                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return false;
                }

                // Count every verification attempt, including malformed codes.
                accessCode.AttemptCount++;

                var isValidFormat =
                    !string.IsNullOrWhiteSpace(code) &&
                    code.Length == CodeLength &&
                    code.All(c => c >= '0' && c <= '9');

                var isValid = false;

                if (isValidFormat)
                {
                    var submittedHash = GenerateCodeHash(
                        accessCode.AppointmentId,
                        code);

                    var storedHash = Convert.FromHexString(
                        accessCode.CodeHash);

                    var calculatedHash = Convert.FromHexString(
                        submittedHash);

                    isValid = CryptographicOperations.FixedTimeEquals(
                        storedHash,
                        calculatedHash);
                }

                if (isValid)
                {
                    // One-time use.
                    accessCode.UsedAt = now;
                }
                else if (accessCode.AttemptCount >= _options.MaxAttempts)
                {
                    // Lock the code after the maximum number of attempts.
                    accessCode.UsedAt = now;
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return isValid;
            });
        }

        private string GenerateCodeHash(long appointmentId, string code)
        {
            var value = Encoding.UTF8.GetBytes(
                $"{appointmentId}:{code}");

            var hash = HMACSHA256.HashData(_hmacKey, value);

            return Convert.ToHexString(hash);
        }
    }
}