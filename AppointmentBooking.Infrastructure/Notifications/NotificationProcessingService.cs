
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Interfaces.Notifications;
using AppointmentBooking.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace AppointmentBooking.Infrastructure.Notifications
{
    public class NotificationProcessingService
        : INotificationProcessingService
    {
        private const int BatchSize = 20;
        private const int MaxAttempts = 3;

        private static readonly TimeSpan ProcessingTimeout =
            TimeSpan.FromMinutes(5);

        private readonly AppointmentBookingDbContext _context;
        private readonly IEmailNotificationSender _emailSender;
        private readonly ISmsNotificationSender _smsSender;
        private readonly ILogger<NotificationProcessingService> _logger;
        private const string AccessCodeProtectorPurpose =
    "AppointmentBooking.AppointmentAccessCode.Notification.v1";

        private readonly IDataProtector _accessCodeProtector;
        public NotificationProcessingService(
            AppointmentBookingDbContext context,
            IEmailNotificationSender emailSender,
            ISmsNotificationSender smsSender,
            ILogger<NotificationProcessingService> logger, IDataProtectionProvider dataProtectionProvider)
        {
            _context = context;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = logger;
            _accessCodeProtector = dataProtectionProvider.CreateProtector(
       AccessCodeProtectorPurpose);
        }

        public async Task<int> ProcessPendingNotificationsAsync(
            CancellationToken cancellationToken = default)
        {
            var claims = await ClaimNotificationsAsync(
                cancellationToken);

            var processedCount = 0;

            foreach (var claim in claims)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var notification = await _context
                    .Set<AppointmentNotification>()
                    .FirstOrDefaultAsync(
                        x => x.NotificationId == claim.NotificationId,
                        cancellationToken);

                if (notification == null)
                    continue;

                var attempt = await _context
                    .Set<AppointmentNotificationAttempt>()
                    .FirstOrDefaultAsync(
                        x => x.NotificationId == claim.NotificationId &&
                             x.AttemptNumber == claim.AttemptNumber,
                        cancellationToken);

                if (attempt == null)
                {
                    _logger.LogError(
                        "Attempt record missing for notification {NotificationId}, attempt {AttemptNumber}.",
                        claim.NotificationId,
                        claim.AttemptNumber);

                    continue;
                }

                try
                {
                    var message = notification.Message;

                    if (string.Equals(
                        notification.NotificationType,
                        "APPOINTMENT_ACCESS_CODE",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        message = _accessCodeProtector.Unprotect(
                            notification.Message);
                    }
                    string? providerReference;

                    switch (notification.Channel.Trim().ToUpperInvariant())
                    {
                        case "EMAIL":
                            providerReference = await _emailSender.SendAsync(
                                notification.Recipient,
                                notification.Subject ?? string.Empty,
                                message,
                                cancellationToken);
                            break;

                        case "SMS":
                            providerReference = await _smsSender.SendAsync(
                                notification.Recipient,
                                message,
                                cancellationToken);
                            break;

                        default:
                            throw new InvalidOperationException(
                                $"Unsupported notification channel: {notification.Channel}");
                    }

                    var completedAt = DateTime.UtcNow;

                    // Update the attempt.
                    attempt.Status = "SENT";
                    attempt.CompletedAt = completedAt;
                    attempt.ErrorMessage = null;
                    attempt.ProviderReference =
    string.IsNullOrWhiteSpace(providerReference)
        ? null
        : providerReference.Length > 150
            ? providerReference[..150]
            : providerReference;
                    // Update the notification.
                    notification.Status = "SENT";
                    notification.SentAt = completedAt;
                    notification.ProcessingStartedAt = null;
                    notification.NextAttemptAt = null;
                    notification.LastError = null;

                    await _context.SaveChangesAsync(cancellationToken);

                    processedCount++;

                    _logger.LogInformation(
                        "Notification {NotificationId}, attempt {AttemptNumber}, sent successfully.",
                        notification.NotificationId,
                        attempt.AttemptNumber);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    // Leave both records PROCESSING.
                    // The stale-processing recovery will mark the
                    // attempt FAILED and allow a subsequent retry.
                    throw;
                }
                catch (Exception ex)
                {
                    var completedAt = DateTime.UtcNow;

                    var errorMessage = ex.Message.Length > 2000
                        ? ex.Message[..2000]
                        : ex.Message;

                    // Update the attempt audit record.
                    attempt.Status = "FAILED";
                    attempt.CompletedAt = completedAt;
                    attempt.ErrorMessage = errorMessage;

                    // Schedule retry or permanently fail.
                    if (notification.AttemptCount >= MaxAttempts)
                    {
                        notification.Status = "FAILED";
                        notification.NextAttemptAt = null;
                        notification.Message = string.Empty;
                    }
                    else
                    {
                        notification.Status = "PENDING";

                        var retryDelayMinutes = (int)Math.Pow(
                            2,
                            notification.AttemptCount - 1);

                        notification.NextAttemptAt =
                            completedAt.AddMinutes(retryDelayMinutes);
                    }

                    notification.ProcessingStartedAt = null;
                    notification.LastError = errorMessage;

                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogError(
                        ex,
                        "Failed to send notification {NotificationId}. Attempt {AttemptNumber} of {MaxAttempts}.",
                        notification.NotificationId,
                        attempt.AttemptNumber,
                        MaxAttempts);
                }
            }

            return processedCount;
        }

        private async Task<List<NotificationClaim>> ClaimNotificationsAsync(
            CancellationToken cancellationToken)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                _context.ChangeTracker.Clear();

                await using var transaction =
                    await _context.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);

                var now = DateTime.UtcNow;
                var staleBefore = now - ProcessingTimeout;

                var notifications = await _context
                    .Set<AppointmentNotification>()
                    .Where(x =>
                        (x.Status == "PENDING" &&
                         (x.NextAttemptAt == null ||
                          x.NextAttemptAt <= now))
                        ||
                        (x.Status == "PROCESSING" &&
                         (x.ProcessingStartedAt == null ||
                          x.ProcessingStartedAt <= staleBefore)))
                    .OrderBy(x => x.CreatedAt)
                    .Take(BatchSize)
                    .ToListAsync(cancellationToken);

                // Identify notifications whose previous processing
                // attempt has timed out.
                var staleNotificationIds = notifications
                    .Where(x => x.Status == "PROCESSING")
                    .Select(x => x.NotificationId)
                    .ToList();

                if (staleNotificationIds.Count > 0)
                {
                    var abandonedAttempts = await _context
                        .Set<AppointmentNotificationAttempt>()
                        .Where(x =>
                            staleNotificationIds.Contains(x.NotificationId) &&
                            x.Status == "PROCESSING")
                        .ToListAsync(cancellationToken);

                    foreach (var abandonedAttempt in abandonedAttempts)
                    {
                        abandonedAttempt.Status = "FAILED";
                        abandonedAttempt.CompletedAt = now;
                        abandonedAttempt.ErrorMessage =
                            "Processing timed out before the attempt completed.";
                    }
                }

                var claims = new List<NotificationClaim>();

                foreach (var notification in notifications)
                {
                    if (notification.AttemptCount >= MaxAttempts)
                    {
                        notification.Status = "FAILED";
                        notification.ProcessingStartedAt = null;
                        notification.NextAttemptAt = null;
                        notification.Message = string.Empty;

                        continue;
                    }

                    notification.AttemptCount++;
                    notification.Status = "PROCESSING";
                    notification.LastAttemptAt = now;
                    notification.ProcessingStartedAt = now;
                    notification.NextAttemptAt = null;

                    // Create the audit record in the same transaction
                    // as the notification claim.
                    var attempt = new AppointmentNotificationAttempt
                    {
                        NotificationId = notification.NotificationId,
                        AttemptNumber = notification.AttemptCount,
                        Status = "PROCESSING",
                        StartedAt = now
                    };

                    _context.Set<AppointmentNotificationAttempt>()
                        .Add(attempt);

                    claims.Add(new NotificationClaim(
                        notification.NotificationId,
                        notification.AttemptCount));
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return claims;
            });
        }

        private sealed record NotificationClaim(
            long NotificationId,
            int AttemptNumber);
    }
}
