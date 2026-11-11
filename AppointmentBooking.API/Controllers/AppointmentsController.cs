using AppointmentBooking.Core.DTOs.Appointments;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Interfaces.Appointments;
using AppointmentBooking.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace AppointmentBooking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly AppointmentBookingDbContext _context;
        private readonly IAppointmentAccessCodeService _accessCodeService;
        private readonly IAppointmentAccessSessionService _accessSessionService;
        public AppointmentsController(AppointmentBookingDbContext context, IAppointmentAccessCodeService accessCodeService, IAppointmentAccessSessionService accessSessionService)
        {
            _context = context;
            _accessCodeService= accessCodeService;
            _accessSessionService=accessSessionService;
        }

        // POST: api/appointments
        [HttpPost]
        [ProducesResponseType(typeof(AppointmentResponse),
            StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]

        public async Task<ActionResult<AppointmentResponse>> CreateAppointment(
    CreateAppointmentRequest request,
    CancellationToken cancellationToken)
        {
            // 1. Validate customer.
            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.CustomerId == request.CustomerId && x.IsActive,
                    cancellationToken);

            if (customer == null)
            {
                return NotFound(new
                {
                    message = "Active customer was not found."
                });
            }

            // 2. Find the PENDING appointment status.
            var pendingStatus = await _context.AppointmentStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Code == "PENDING",
                    cancellationToken);

            if (pendingStatus == null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message = "The PENDING appointment status has not been configured."
                    });
            }

            var appointmentReference = Guid.NewGuid();

            // 3. Execute booking inside SQL Server's retrying execution strategy.
            var strategy = _context.Database.CreateExecutionStrategy();

            BookingResult bookingResult;

            try
            {
                bookingResult = await strategy.ExecuteAsync(async () =>
                {
                    // Ensure a retry starts with a clean change tracker.
                    _context.ChangeTracker.Clear();

                    await using var transaction =
                        await _context.Database.BeginTransactionAsync(
                            IsolationLevel.Serializable,
                            cancellationToken);

                    // 4. Load the selected slot.
                    var slot = await _context.AppointmentSlots
                        .FirstOrDefaultAsync(
                            x => x.SlotId == request.SlotId,
                            cancellationToken);

                    if (slot == null)
                    {
                        return BookingResult.Failed("SLOT_NOT_FOUND");
                    }

                    if (!slot.IsActive)
                    {
                        return BookingResult.Failed("SLOT_INACTIVE");
                    }

                    var now = DateTime.UtcNow;

                    if (slot.StartDateTime <= now)
                    {
                        return BookingResult.Failed("SLOT_IN_PAST");
                    }

                    if (slot.BookedCount >= slot.Capacity)
                    {
                        return BookingResult.Failed("SLOT_FULL");
                    }

                    // 5. Create appointment.
                    var appointment = new Appointment
                    {
                        AppointmentReference = appointmentReference,
                        CustomerId = customer.CustomerId,
                        SlotId = slot.SlotId,
                        AppointmentStatusId =
                            pendingStatus.AppointmentStatusId,
                        BookingDate = now,
                        CustomerNotes = request.CustomerNotes?.Trim(),
                        CreatedAt = now
                    };

                    // 6. Reserve one place in the slot.
                    slot.BookedCount++;

                    _context.Appointments.Add(appointment);

                    // 7. Record appointment history.
                    var history = new AppointmentHistory
                    {
                        Appointment = appointment,
                        OldStatusId = null,
                        NewStatusId = pendingStatus.AppointmentStatusId,
                        ChangedAt = now,
                        ChangedBy = "CUSTOMER",
                        Comments = "Appointment created."
                    };

                    _context.AppointmentHistory.Add(history);

                    // 8. Queue confirmation notifications.
                    // These are PENDING records; no email or SMS is sent here.

                    var notificationMessage =
                        $"Your appointment has been booked. " +
                        $"Reference: {appointmentReference}.";

                    if (!string.IsNullOrWhiteSpace(customer.Email))
                    {
                        _context.AppointmentNotifications.Add(
                            new AppointmentNotification
                            {
                                Appointment = appointment,
                                NotificationType = "APPOINTMENT_CONFIRMATION",
                                Channel = "EMAIL",
                                Recipient = customer.Email,
                                Subject = "Appointment Booking Confirmation",
                                Message = notificationMessage,
                                Status = "PENDING",
                                CreatedAt = now
                            });
                    }

                    if (!string.IsNullOrWhiteSpace(customer.PhoneNumber))
                    {
                        _context.AppointmentNotifications.Add(
                            new AppointmentNotification
                            {
                                Appointment = appointment,
                                NotificationType = "APPOINTMENT_CONFIRMATION",
                                Channel = "SMS",
                                Recipient = customer.PhoneNumber,
                                Subject = null,
                                Message = notificationMessage,
                                Status = "PENDING",
                                CreatedAt = now
                            });
                    }

                    // 9. Save everything atomically.
                    await _context.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    // 10. Build response.
                    var response = new AppointmentResponse
                    {
                        AppointmentId = appointment.AppointmentId,
                        AppointmentReference =
                            appointment.AppointmentReference,
                        CustomerId = appointment.CustomerId,
                        SlotId = slot.SlotId,
                        BranchId = slot.BranchId,
                        ServiceId = slot.ServiceId,
                        StartDateTime = DateTime.SpecifyKind(
                            slot.StartDateTime, DateTimeKind.Utc),
                        EndDateTime = DateTime.SpecifyKind(
                            slot.EndDateTime, DateTimeKind.Utc),
                        StatusCode = "PENDING",
                        StatusName = pendingStatus.Name,
                        BookingDate = appointment.BookingDate,
                        CustomerNotes = appointment.CustomerNotes
                    };

                    return BookingResult.Succeeded(response);
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new
                {
                    message = "This appointment slot was just updated by another booking. Please select an available slot."
                });
            }

            // 11. Handle booking failures.
            if (!bookingResult.Success)
            {
                return bookingResult.ErrorCode switch
                {
                    "SLOT_NOT_FOUND" => NotFound(new
                    {
                        message = "Appointment slot was not found."
                    }),

                    "SLOT_INACTIVE" => Conflict(new
                    {
                        message = "This appointment slot is no longer active."
                    }),

                    "SLOT_IN_PAST" => Conflict(new
                    {
                        message = "Appointments cannot be booked for past time slots."
                    }),

                    "SLOT_FULL" => Conflict(new
                    {
                        message = "This appointment slot is fully booked."
                    }),

                    _ => StatusCode(
                        StatusCodes.Status500InternalServerError,
                        new
                        {
                            message = "Unable to create the appointment."
                        })
                };
            }

            return StatusCode(
                StatusCodes.Status201Created,
                bookingResult.Response);
        }


        // GET: api/appointments/customer/{customerId}?reference={appointmentReference}
        [HttpGet("customer/{customerId:int}")]
        [ProducesResponseType(
            typeof(IEnumerable<AppointmentResponse>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<AppointmentResponse>>>
            GetCustomerAppointments(
                int customerId,
                [FromQuery] Guid reference,
                CancellationToken cancellationToken)
        {
            var authorizedAppointmentId =
                await GetAuthorizedAppointmentIdAsync(
                    reference,
                    cancellationToken);

            if (authorizedAppointmentId == null)
            {
                return Unauthorized(new
                {
                    message = "A valid appointment access token is required."
                });
            }

            var appointments = await (
                from a in _context.Appointments.AsNoTracking()
                join status in _context.AppointmentStatuses.AsNoTracking()
                    on a.AppointmentStatusId equals status.AppointmentStatusId
                join slot in _context.AppointmentSlots.AsNoTracking()
                    on a.SlotId equals slot.SlotId
                where a.CustomerId == customerId &&
                      a.AppointmentId == authorizedAppointmentId.Value &&
                      a.AppointmentReference == reference
                orderby a.BookingDate descending
                select new AppointmentResponse
                {
                    AppointmentId = a.AppointmentId,
                    AppointmentReference = a.AppointmentReference,
                    CustomerId = a.CustomerId,
                    SlotId = a.SlotId,
                    BranchId = slot.BranchId,
                    ServiceId = slot.ServiceId,
                    StartDateTime = slot.StartDateTime,
                    EndDateTime = slot.EndDateTime,
                    StatusCode = status.Code,
                    StatusName = status.Name,
                    BookingDate = a.BookingDate,
                    CustomerNotes = a.CustomerNotes
                })
                .ToListAsync(cancellationToken);

            foreach (var appointment in appointments)
            {
                appointment.StartDateTime = DateTime.SpecifyKind(
                    appointment.StartDateTime,
                    DateTimeKind.Utc);

                appointment.EndDateTime = DateTime.SpecifyKind(
                    appointment.EndDateTime,
                    DateTimeKind.Utc);

                appointment.BookingDate = DateTime.SpecifyKind(
                    appointment.BookingDate,
                    DateTimeKind.Utc);
            }

            return Ok(appointments);
        }

        // POST: api/appointments/reference/{reference}/cancel
        [HttpPost("reference/{reference:guid}/cancel")]
        [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AppointmentResponse>> CancelAppointment(
            Guid reference,
            CancelAppointmentRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.CancellationReason))
            {
                return BadRequest(new
                {
                    message = "Cancellation reason is required."
                });
            }

            var cancelledStatus = await _context.AppointmentStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Code == "CANCELLED",
                    cancellationToken);

            if (cancelledStatus == null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = "CANCELLED appointment status is not configured." });
            }
            var authorizedAppointmentId =
    await GetAuthorizedAppointmentIdAsync(
        reference,
        cancellationToken);

            if (authorizedAppointmentId == null)
            {
                return Unauthorized(new
                {
                    message = "A valid appointment access token is required."
                });
            }
            var strategy = _context.Database.CreateExecutionStrategy();

            var result = await strategy.ExecuteAsync(async () =>
            {
                _context.ChangeTracker.Clear();

                await using var transaction =
                    await _context.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);
                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(
                        x =>
                            x.AppointmentReference == reference &&
                            x.AppointmentId == authorizedAppointmentId.Value,
                        cancellationToken);

                if (appointment == null)
                {
                    return CancellationResult.Failed("APPOINTMENT_NOT_FOUND");
                }

                var currentStatus = await _context.AppointmentStatuses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.AppointmentStatusId == appointment.AppointmentStatusId,
                        cancellationToken);

                if (currentStatus == null)
                {
                    return CancellationResult.Failed("INVALID_STATUS");
                }

                // Only pending or confirmed appointments may be cancelled.
                if (currentStatus.Code != "PENDING" &&
                    currentStatus.Code != "CONFIRMED")
                {
                    return CancellationResult.Failed("NOT_CANCELLABLE");
                }

                var slot = await _context.AppointmentSlots
                    .FirstOrDefaultAsync(
                        x => x.SlotId == appointment.SlotId,
                        cancellationToken);

                if (slot == null)
                {
                    return CancellationResult.Failed("SLOT_NOT_FOUND");
                }

                // Release the reserved slot capacity.
                if (slot.BookedCount <= 0)
                {
                    return CancellationResult.Failed("INVALID_SLOT_CAPACITY");
                }

                var customer = await _context.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.CustomerId == appointment.CustomerId,
                        cancellationToken);

                if (customer == null)
                {
                    return CancellationResult.Failed("CUSTOMER_NOT_FOUND");
                }

                var now = DateTime.UtcNow;

                var oldStatusId = appointment.AppointmentStatusId;

                // Update appointment.
                appointment.AppointmentStatusId = cancelledStatus.AppointmentStatusId;

                appointment.CancellationReason = request.CancellationReason.Trim();

                appointment.CancelledAt = now;
                appointment.UpdatedAt = now;

                // Revoke all active customer access sessions.
                var activeSessions = await _context.AppointmentAccessSessions
                    .Where(x =>
                        x.AppointmentId == appointment.AppointmentId &&
                        x.RevokedAt == null)
                    .ToListAsync(cancellationToken);

                foreach (var session in activeSessions)
                {
                    session.RevokedAt = now;
                }

                slot.BookedCount--;

                // Record appointment history.
                _context.AppointmentHistory.Add(
                    new AppointmentHistory
                    {
                        //AppointmentId = appointment.AppointmentId,
                        Appointment = appointment,
                        OldStatusId = oldStatusId,
                        NewStatusId = cancelledStatus.AppointmentStatusId,
                        ChangedAt = now,
                        ChangedBy = "CUSTOMER",
                        Comments = request.CancellationReason.Trim()
                    });

                // Queue cancellation notifications.
                var message =
                    $"Your appointment {appointment.AppointmentReference} " +
                    $"has been cancelled. Reason: {request.CancellationReason.Trim()}";

                if (!string.IsNullOrWhiteSpace(customer.Email))
                {
                    _context.AppointmentNotifications.Add(
                        new AppointmentNotification
                        {
                            //AppointmentId = appointment.AppointmentId,
                            Appointment = appointment,
                            NotificationType = "APPOINTMENT_CANCELLATION",
                            Channel = "EMAIL",
                            Recipient = customer.Email,
                            Subject = "Appointment Cancellation",
                            Message = message,
                            Status = "PENDING",
                            CreatedAt = now
                        });
                }

                if (!string.IsNullOrWhiteSpace(customer.PhoneNumber))
                {
                    _context.AppointmentNotifications.Add(
                        new AppointmentNotification
                        {
                            AppointmentId = appointment.AppointmentId,
                            NotificationType = "APPOINTMENT_CANCELLATION",
                            Channel = "SMS",
                            Recipient = customer.PhoneNumber,
                            Subject = null,
                            Message = message,
                            Status = "PENDING",
                            CreatedAt = now
                        });
                }

                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                var response = new AppointmentResponse
                {
                    AppointmentId = appointment.AppointmentId,
                    AppointmentReference = appointment.AppointmentReference,
                    CustomerId = appointment.CustomerId,
                    SlotId = slot.SlotId,
                    BranchId = slot.BranchId,
                    ServiceId = slot.ServiceId,
                    StartDateTime = DateTime.SpecifyKind(
                        slot.StartDateTime, DateTimeKind.Utc),
                    EndDateTime = DateTime.SpecifyKind(
                        slot.EndDateTime, DateTimeKind.Utc),
                    StatusCode = cancelledStatus.Code,
                    StatusName = cancelledStatus.Name,
                    BookingDate = DateTime.SpecifyKind(
                        appointment.BookingDate, DateTimeKind.Utc),
                    CustomerNotes = appointment.CustomerNotes
                };

                return CancellationResult.Succeeded(response);
            });

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "APPOINTMENT_NOT_FOUND" => NotFound(new
                    {
                        message = "Appointment was not found."
                    }),

                    "SLOT_NOT_FOUND" => Conflict(new
                    {
                        message = "The appointment slot could not be found."
                    }),

                    "CUSTOMER_NOT_FOUND" => Conflict(new
                    {
                        message = "The appointment customer could not be found."
                    }),
                    "INVALID_STATUS" => Conflict(new
                    {
                        message = "The appointment has an invalid status configuration."
                    }),

                    "NOT_CANCELLABLE" => Conflict(new
                    {
                        message = "This appointment cannot be cancelled because of its current status."
                    }),

                    "INVALID_SLOT_CAPACITY" => Conflict(new
                    {
                        message = "The slot booking count is inconsistent. Please contact support."
                    }),

                    _ => StatusCode(
                        StatusCodes.Status500InternalServerError,
                        new { message = "Unable to cancel the appointment." })
                };
            }

            return Ok(result.Response);
        }

        // POST: api/appointments/access-code/request
        [HttpPost("access-code/request")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> RequestAccessCode( RequestAppointmentAccessCodeRequest request,
            CancellationToken cancellationToken)
        {
            await _accessCodeService.RequestAccessCodeAsync(
                request.AppointmentReference,
                request.Email,
                cancellationToken);

            // Always return the same response, whether or not the
            // appointment and email combination exists.
            return Ok(new
            {
                message = "If the appointment details are valid, an access code will be sent to the registered email address."
            });
        }

        private async Task<long?> GetAuthorizedAppointmentIdAsync(
    Guid reference,
    CancellationToken cancellationToken)
        {
            var authorizationHeader = Request.Headers.Authorization.ToString();

            if (string.IsNullOrWhiteSpace(authorizationHeader) ||
                !authorizationHeader.StartsWith(
                    "Bearer ",
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var token = authorizationHeader["Bearer ".Length..].Trim();

            if (string.IsNullOrWhiteSpace(token))
                return null;

            return await _accessSessionService.ValidateSessionAsync(
                reference,
                token,
                cancellationToken);
        }

        // POST: api/appointments/access-code/verify
        [HttpPost("access-code/verify")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyAccessCode(
            VerifyAppointmentAccessCodeRequest request,
            CancellationToken cancellationToken)
        {
            var isValid = await _accessCodeService.VerifyAccessCodeAsync(
                request.AppointmentReference,
                request.Email,
                request.Code,
                cancellationToken);

            if (!isValid)
            {
                return BadRequest(new
                {
                    message = "The access code is invalid or has expired."
                });
            }

            // Create a session only after successful OTP verification.
            var accessToken = await _accessSessionService.CreateSessionAsync(
                request.AppointmentReference,
                request.Email,
                cancellationToken);

            if (accessToken == null)
            {
                return BadRequest(new
                {
                    message = "Unable to establish an appointment access session."
                });
            }

            return Ok(new
            {
                verified = true,
                accessToken,
                tokenType = "Bearer",
                expiresIn = 900,
                message = "Access code verified successfully."
            });
        }

        private sealed record CancellationResult(
            bool Success,
            string? ErrorCode,
            AppointmentResponse? Response)
        {
            public static CancellationResult Failed(string errorCode)
                => new(false, errorCode, null);

            public static CancellationResult Succeeded(
                AppointmentResponse response)
                => new(true, null, response);
        }

        // POST: api/appointments/reference/{reference}/confirm
        [Authorize(Roles = "Staff,Admin")]
        [HttpPost("reference/{reference:guid}/confirm")]
        [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AppointmentResponse>> ConfirmAppointment(
            Guid reference,
            ConfirmAppointmentRequest request,
            CancellationToken cancellationToken)
        {
            var confirmedBy =
                User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(confirmedBy))
            {
                return Unauthorized(new
                {
                    message = "Authenticated staff identity could not be determined."
                });
            }

            var confirmedStatus = await _context.AppointmentStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Code == "CONFIRMED",
                    cancellationToken);

            if (confirmedStatus == null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = "CONFIRMED appointment status is not configured." });
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            var result = await strategy.ExecuteAsync(async () =>
            {
                _context.ChangeTracker.Clear();

                await using var transaction =
                    await _context.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);

                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(
                        x => x.AppointmentReference == reference,
                        cancellationToken);

                if (appointment == null)
                {
                    return ConfirmationResult.Failed("APPOINTMENT_NOT_FOUND");
                }

                var currentStatus = await _context.AppointmentStatuses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.AppointmentStatusId == appointment.AppointmentStatusId,
                        cancellationToken);

                if (currentStatus == null)
                {
                    return ConfirmationResult.Failed("INVALID_STATUS");
                }

                if (currentStatus.Code != "PENDING")
                {
                    return ConfirmationResult.Failed("NOT_CONFIRMABLE");
                }

                var slot = await _context.AppointmentSlots
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.SlotId == appointment.SlotId,
                        cancellationToken);

                if (slot == null)
                {
                    return ConfirmationResult.Failed("SLOT_NOT_FOUND");
                }

                var customer = await _context.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.CustomerId == appointment.CustomerId,
                        cancellationToken);

                if (customer == null)
                {
                    return ConfirmationResult.Failed("CUSTOMER_NOT_FOUND");
                }

                var now = DateTime.UtcNow;

                var oldStatusId = appointment.AppointmentStatusId;

                appointment.AppointmentStatusId =
                    confirmedStatus.AppointmentStatusId;

                _context.AppointmentHistory.Add(
                    new AppointmentHistory
                    {
                        Appointment = appointment,
                        OldStatusId = oldStatusId,
                        NewStatusId = confirmedStatus.AppointmentStatusId,
                        ChangedAt = now,
                        ChangedBy = confirmedBy,
                        Comments = string.IsNullOrWhiteSpace(request.Comments)
                            ? "Appointment confirmed."
                            : request.Comments.Trim()
                    });

                var message =
                    $"Your bank appointment has been confirmed. " +
                    $"Reference: {appointment.AppointmentReference}.";

                if (!string.IsNullOrWhiteSpace(customer.Email))
                {
                    _context.AppointmentNotifications.Add(
                        new AppointmentNotification
                        {
                            Appointment = appointment,
                            NotificationType = "APPOINTMENT_CONFIRMATION",
                            Channel = "EMAIL",
                            Recipient = customer.Email,
                            Subject = "Appointment Confirmed",
                            Message = message,
                            Status = "PENDING",
                            CreatedAt = now
                        });
                }

                if (!string.IsNullOrWhiteSpace(customer.PhoneNumber))
                {
                    _context.AppointmentNotifications.Add(
                        new AppointmentNotification
                        {
                            Appointment = appointment,
                            NotificationType = "APPOINTMENT_CONFIRMATION",
                            Channel = "SMS",
                            Recipient = customer.PhoneNumber,
                            Subject = null,
                            Message = message,
                            Status = "PENDING",
                            CreatedAt = now
                        });
                }

                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                var response = new AppointmentResponse
                {
                    AppointmentId = appointment.AppointmentId,
                    AppointmentReference = appointment.AppointmentReference,
                    CustomerId = appointment.CustomerId,
                    SlotId = slot.SlotId,
                    BranchId = slot.BranchId,
                    ServiceId = slot.ServiceId,
                    StartDateTime = DateTime.SpecifyKind(
                        slot.StartDateTime, DateTimeKind.Utc),
                    EndDateTime = DateTime.SpecifyKind(
                        slot.EndDateTime, DateTimeKind.Utc),
                    StatusCode = confirmedStatus.Code,
                    StatusName = confirmedStatus.Name,
                    BookingDate = DateTime.SpecifyKind(
                        appointment.BookingDate, DateTimeKind.Utc),
                    CustomerNotes = appointment.CustomerNotes
                };

                return ConfirmationResult.Succeeded(response);
            });

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "APPOINTMENT_NOT_FOUND" => NotFound(new
                    {
                        message = "Appointment was not found."
                    }),

                    "NOT_CONFIRMABLE" => Conflict(new
                    {
                        message = "Only pending appointments can be confirmed."
                    }),

                    "SLOT_NOT_FOUND" => Conflict(new
                    {
                        message = "The appointment slot could not be found."
                    }),

                    "CUSTOMER_NOT_FOUND" => Conflict(new
                    {
                        message = "The appointment customer could not be found."
                    }),

                    _ => StatusCode(
                        StatusCodes.Status500InternalServerError,
                        new { message = "Unable to confirm the appointment." })
                };
            }

            return Ok(result.Response);
        }


        // POST: api/admin/appointments/{appointmentId}/cancel
        [Authorize(Roles = "Staff,Admin")]
        [HttpPost("~/api/admin/appointments/{appointmentId:long}/cancel")]
        [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AppointmentResponse>> CancelAdminAppointment(
            long appointmentId,
            CancelAppointmentRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.CancellationReason))
            {
                return BadRequest(new
                {
                    message = "Cancellation reason is required."
                });
            }

            var cancelledBy =
                User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(cancelledBy))
            {
                return Unauthorized(new
                {
                    message = "Authenticated staff identity could not be determined."
                });
            }

            var cancelledStatus = await _context.AppointmentStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Code == "CANCELLED",
                    cancellationToken);

            if (cancelledStatus == null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = "CANCELLED appointment status is not configured." });
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            var result = await strategy.ExecuteAsync(async () =>
            {
                _context.ChangeTracker.Clear();

                await using var transaction =
                    await _context.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);

                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(
                        x => x.AppointmentId == appointmentId,
                        cancellationToken);

                if (appointment == null)
                {
                    return CancellationResult.Failed("APPOINTMENT_NOT_FOUND");
                }

                var currentStatus = await _context.AppointmentStatuses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.AppointmentStatusId == appointment.AppointmentStatusId,
                        cancellationToken);

                if (currentStatus == null)
                {
                    return CancellationResult.Failed("INVALID_STATUS");
                }

                if (currentStatus.Code != "PENDING" &&
                    currentStatus.Code != "CONFIRMED")
                {
                    return CancellationResult.Failed("NOT_CANCELLABLE");
                }

                var slot = await _context.AppointmentSlots
                    .FirstOrDefaultAsync(
                        x => x.SlotId == appointment.SlotId,
                        cancellationToken);

                if (slot == null)
                {
                    return CancellationResult.Failed("SLOT_NOT_FOUND");
                }

                if (slot.BookedCount <= 0)
                {
                    return CancellationResult.Failed("INVALID_SLOT_CAPACITY");
                }

                var customer = await _context.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.CustomerId == appointment.CustomerId,
                        cancellationToken);

                if (customer == null)
                {
                    return CancellationResult.Failed("CUSTOMER_NOT_FOUND");
                }

                var now = DateTime.UtcNow;
                var oldStatusId = appointment.AppointmentStatusId;
                var reason = request.CancellationReason.Trim();

                appointment.AppointmentStatusId =
                    cancelledStatus.AppointmentStatusId;

                appointment.CancellationReason = reason;
                appointment.CancelledAt = now;
                appointment.UpdatedAt = now;

                // Revoke active customer access sessions.
                var activeSessions = await _context.AppointmentAccessSessions
                    .Where(x =>
                        x.AppointmentId == appointment.AppointmentId &&
                        x.RevokedAt == null)
                    .ToListAsync(cancellationToken);

                foreach (var session in activeSessions)
                {
                    session.RevokedAt = now;
                }

                // Release reserved slot capacity.
                slot.BookedCount--;

                // Record staff cancellation in appointment history.
                _context.AppointmentHistory.Add(
                    new AppointmentHistory
                    {
                        Appointment = appointment,
                        OldStatusId = oldStatusId,
                        NewStatusId = cancelledStatus.AppointmentStatusId,
                        ChangedAt = now,
                        ChangedBy = cancelledBy,
                        Comments = reason
                    });

                // Queue customer notifications.
                var message =
                    $"Your appointment {appointment.AppointmentReference} " +
                    $"has been cancelled. Reason: {reason}";

                if (!string.IsNullOrWhiteSpace(customer.Email))
                {
                    _context.AppointmentNotifications.Add(
                        new AppointmentNotification
                        {
                            Appointment = appointment,
                            NotificationType = "APPOINTMENT_CANCELLATION",
                            Channel = "EMAIL",
                            Recipient = customer.Email,
                            Subject = "Appointment Cancellation",
                            Message = message,
                            Status = "PENDING",
                            CreatedAt = now
                        });
                }

                if (!string.IsNullOrWhiteSpace(customer.PhoneNumber))
                {
                    _context.AppointmentNotifications.Add(
                        new AppointmentNotification
                        {
                            Appointment = appointment,
                            NotificationType = "APPOINTMENT_CANCELLATION",
                            Channel = "SMS",
                            Recipient = customer.PhoneNumber,
                            Subject = null,
                            Message = message,
                            Status = "PENDING",
                            CreatedAt = now
                        });
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var response = new AppointmentResponse
                {
                    AppointmentId = appointment.AppointmentId,
                    AppointmentReference = appointment.AppointmentReference,
                    CustomerId = appointment.CustomerId,
                    SlotId = slot.SlotId,
                    BranchId = slot.BranchId,
                    ServiceId = slot.ServiceId,
                    StartDateTime = DateTime.SpecifyKind(
                        slot.StartDateTime, DateTimeKind.Utc),
                    EndDateTime = DateTime.SpecifyKind(
                        slot.EndDateTime, DateTimeKind.Utc),
                    StatusCode = cancelledStatus.Code,
                    StatusName = cancelledStatus.Name,
                    BookingDate = DateTime.SpecifyKind(
                        appointment.BookingDate, DateTimeKind.Utc),
                    CustomerNotes = appointment.CustomerNotes
                };

                return CancellationResult.Succeeded(response);
            });

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "APPOINTMENT_NOT_FOUND" => NotFound(new
                    {
                        message = "Appointment was not found."
                    }),

                    "INVALID_STATUS" => Conflict(new
                    {
                        message = "The appointment has an invalid status configuration."
                    }),

                    "NOT_CANCELLABLE" => Conflict(new
                    {
                        message = "This appointment cannot be cancelled because of its current status."
                    }),

                    "SLOT_NOT_FOUND" => Conflict(new
                    {
                        message = "The appointment slot could not be found."
                    }),

                    "CUSTOMER_NOT_FOUND" => Conflict(new
                    {
                        message = "The appointment customer could not be found."
                    }),

                    "INVALID_SLOT_CAPACITY" => Conflict(new
                    {
                        message = "The slot booking count is inconsistent. Please contact support."
                    }),

                    _ => StatusCode(
                        StatusCodes.Status500InternalServerError,
                        new { message = "Unable to cancel the appointment." })
                };
            }

            return Ok(result.Response);
        }


        private sealed record ConfirmationResult(
            bool Success,
            string? ErrorCode,
            AppointmentResponse? Response)
        {
            public static ConfirmationResult Failed(string errorCode)
                => new(false, errorCode, null);

            public static ConfirmationResult Succeeded(
                AppointmentResponse response)
                => new(true, null, response);
        }

        // GET: api/appointments/reference/{reference}
        [HttpGet("reference/{reference:guid}")]
        [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AppointmentResponse>> GetByReference(
            Guid reference,
            CancellationToken cancellationToken)
        {
            // 1. Validate appointment access token.
            var authorizedAppointmentId =
                await GetAuthorizedAppointmentIdAsync(
                    reference,
                    cancellationToken);

            if (authorizedAppointmentId == null)
            {
                return Unauthorized(new
                {
                    message = "A valid appointment access token is required."
                });
            }

            // 2. Retrieve only the appointment authorised by the token.
            var appointment = await (
                from a in _context.Appointments.AsNoTracking()
                join status in _context.AppointmentStatuses.AsNoTracking()
                    on a.AppointmentStatusId equals status.AppointmentStatusId
                join slot in _context.AppointmentSlots.AsNoTracking()
                    on a.SlotId equals slot.SlotId
                where a.AppointmentReference == reference &&
                      a.AppointmentId == authorizedAppointmentId.Value
                select new AppointmentResponse
                {
                    AppointmentId = a.AppointmentId,
                    AppointmentReference = a.AppointmentReference,
                    CustomerId = a.CustomerId,
                    SlotId = a.SlotId,
                    BranchId = slot.BranchId,
                    ServiceId = slot.ServiceId,
                    StartDateTime = slot.StartDateTime,
                    EndDateTime = slot.EndDateTime,
                    StatusCode = status.Code,
                    StatusName = status.Name,
                    BookingDate = a.BookingDate,
                    CustomerNotes = a.CustomerNotes
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (appointment == null)
            {
                return NotFound(new
                {
                    message = "Appointment was not found."
                });
            }

            // 3. SQL Server datetime2 does not preserve DateTime.Kind.
            appointment.StartDateTime = DateTime.SpecifyKind(
                appointment.StartDateTime, DateTimeKind.Utc);

            appointment.EndDateTime = DateTime.SpecifyKind(
                appointment.EndDateTime, DateTimeKind.Utc);

            appointment.BookingDate = DateTime.SpecifyKind(
                appointment.BookingDate, DateTimeKind.Utc);

            return Ok(appointment);
        }

        private sealed record BookingResult(
            bool Success,
            string? ErrorCode,
            AppointmentResponse? Response)
        {
            public static BookingResult Failed(string errorCode)
                => new(false, errorCode, null);

            public static BookingResult Succeeded(
                AppointmentResponse response)
                => new(true, null, response);
        }
    }
}
