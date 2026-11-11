using AppointmentBooking.Core.DTOs.Notifications;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AppointmentBooking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly AppointmentBookingDbContext _context;

        public NotificationsController(
            AppointmentBookingDbContext context)
        {
            _context = context;
        }


        // GET: api/notifications/11
        [HttpGet("{notificationId:long}")]
        public async Task<ActionResult<NotificationDetailsDto>> GetNotificationById(
            long notificationId,
            CancellationToken cancellationToken)
        {
            var notification = await _context
                .Set<AppointmentNotification>()
                .AsNoTracking()
                .Where(x => x.NotificationId == notificationId)
                .Select(x => new NotificationDetailsDto
                {
                    NotificationId = x.NotificationId,
                    AppointmentId = x.AppointmentId,
                    NotificationType = x.NotificationType,
                    Channel = x.Channel,
                    Recipient = x.Recipient,
                    Subject = x.Subject,
                    Message = x.Message,
                    Status = x.Status,
                    AttemptCount = x.AttemptCount,
                    CreatedAt = x.CreatedAt,
                    LastAttemptAt = x.LastAttemptAt,
                    ProcessingStartedAt = x.ProcessingStartedAt,
                    NextAttemptAt = x.NextAttemptAt,
                    SentAt = x.SentAt,
                    LastError = x.LastError
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (notification == null)
            {
                return NotFound(
                    $"Notification {notificationId} was not found.");
            }

            return Ok(notification);
        }


        // POST: api/notifications/11/retry
        [HttpPost("{notificationId:long}/retry")]
        public async Task<IActionResult> RetryFailedNotification(
            long notificationId,
            CancellationToken cancellationToken)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            var result = await strategy.ExecuteAsync(async () =>
            {
                _context.ChangeTracker.Clear();

                await using var transaction =
                    await _context.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);

                var notification = await _context
                    .Set<AppointmentNotification>()
                    .FirstOrDefaultAsync(
                        x => x.NotificationId == notificationId,
                        cancellationToken);

                if (notification == null)
                {
                    return (Found: false, Retried: false);
                }

                if (notification.Status != "FAILED")
                {
                    return (Found: true, Retried: false);
                }

                // Reset the notification for a new retry cycle.
                notification.Status = "PENDING";
                notification.AttemptCount = 0;
                notification.NextAttemptAt = null;
                notification.ProcessingStartedAt = null;
                notification.SentAt = null;

                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return (Found: true, Retried: true);
            });

            if (!result.Found)
            {
                return NotFound(
                    $"Notification {notificationId} was not found.");
            }

            if (!result.Retried)
            {
                return Conflict(
                    "Only FAILED notifications can be manually retried.");
            }

            return AcceptedAtAction(
                nameof(GetNotificationById),
                new { notificationId },
                new
                {
                    message = "Notification queued for another delivery attempt.",
                    notificationId,
                    status = "PENDING"
                });
        }


        // GET: api/notifications
        // GET: api/notifications?status=FAILED
        // GET: api/notifications?channel=EMAIL
        // GET: api/notifications?status=FAILED&channel=EMAIL&pageNumber=1&pageSize=10

        [HttpGet]
        public async Task<ActionResult<NotificationListResponseDto>> GetNotifications(
            [FromQuery] string? status,
            [FromQuery] string? channel,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1)
            {
                return BadRequest("Page number must be greater than zero.");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest("Page size must be between 1 and 100.");
            }

            var query = _context
                .Set<AppointmentNotification>()
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                status = status.Trim().ToUpperInvariant();

                var validStatuses = new[]
                {
                    "PENDING",
                    "PROCESSING",
                    "SENT",
                    "FAILED"
                };

                if (!validStatuses.Contains(status))
                {
                    return BadRequest("Invalid notification status.");
                }

                query = query.Where(x => x.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(channel))
            {
                channel = channel.Trim().ToUpperInvariant();

                var validChannels = new[]
                {
                    "EMAIL",
                    "SMS"
                };

                if (!validChannels.Contains(channel))
                {
                    return BadRequest("Invalid notification channel.");
                }

                query = query.Where(x => x.Channel == channel);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var notifications = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.NotificationId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new NotificationListItemDto
                {
                    NotificationId = x.NotificationId,
                    AppointmentId = x.AppointmentId,
                    NotificationType = x.NotificationType,
                    Channel = x.Channel,
                    Recipient = x.Recipient,
                    Subject = x.Subject,
                    Status = x.Status,
                    AttemptCount = x.AttemptCount,
                    CreatedAt = x.CreatedAt,
                    LastAttemptAt = x.LastAttemptAt,
                    NextAttemptAt = x.NextAttemptAt,
                    SentAt = x.SentAt,
                    LastError = x.LastError
                })
                .ToListAsync(cancellationToken);

            var response = new NotificationListResponseDto
            {
                Items = notifications,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(
                    totalCount / (double)pageSize)
            };

            return Ok(response);
        }
    }
}
