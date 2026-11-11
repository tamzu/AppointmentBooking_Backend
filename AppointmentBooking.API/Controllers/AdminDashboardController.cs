using AppointmentBooking.Core.DTOs.Admin;
using AppointmentBooking.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.API.Controllers
{
    [Route("api/admin/dashboard")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly AppointmentBookingDbContext _context;

        private const string PendingStatusCode = "PENDING";
        private const string ConfirmedStatusCode = "CONFIRMED";

        public AdminDashboardController(
            AppointmentBookingDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        [ProducesResponseType(
            typeof(AdminDashboardSummaryResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AdminDashboardSummaryResponse>>
            GetSummary(CancellationToken cancellationToken)
        {
            var southAfricaTimeZone = GetSouthAfricaTimeZone();

            // Determine today's date using South African local time.
            var nowUtc = DateTime.UtcNow;

            var localToday = TimeZoneInfo
                .ConvertTimeFromUtc(nowUtc, southAfricaTimeZone)
                .Date;

            var localTomorrow = localToday.AddDays(1);

            // Convert local day boundaries to UTC for database queries.
            var startOfDayUtc = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(
                    localToday,
                    DateTimeKind.Unspecified),
                southAfricaTimeZone);

            var startOfTomorrowUtc = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(
                    localTomorrow,
                    DateTimeKind.Unspecified),
                southAfricaTimeZone);

            var appointments = _context.Appointments
                .AsNoTracking();

            var todaysAppointments = await appointments
                .CountAsync(
                    a => a.Slot.StartDateTime >= startOfDayUtc
                         && a.Slot.StartDateTime < startOfTomorrowUtc,
                    cancellationToken);

            var pendingAppointments = await appointments
                .CountAsync(
                    a => a.Status.Code == PendingStatusCode,
                    cancellationToken);

            var confirmedAppointments = await appointments
                .CountAsync(
                    a => a.Status.Code == ConfirmedStatusCode,
                    cancellationToken);

            var totalCustomers = await _context.Customers
                .AsNoTracking()
                .CountAsync(cancellationToken);

            var statusSummary = await _context.AppointmentStatuses
                .AsNoTracking()
                .Select(status => new AppointmentStatusSummaryDto
                {
                    StatusCode = status.Code,
                    StatusName = status.Name,
                    Count = status.Appointments.Count()
                })
                .OrderByDescending(status => status.Count)
                .ToListAsync(cancellationToken);

            var recentAppointments = await appointments
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .Select(a => new RecentAppointmentDto
                {
                    AppointmentId = a.AppointmentId,
                    AppointmentReference = a.AppointmentReference,

                    CustomerName =
                        a.Customer.FirstName + " " + a.Customer.LastName,

                    CustomerEmail = a.Customer.Email,
                    CustomerPhone = a.Customer.PhoneNumber,

                    BranchName = a.Slot.Branch.BranchName,
                    ServiceName = a.Slot.Service.ServiceName,

                    StatusCode = a.Status.Code,
                    StatusName = a.Status.Name,

                    AppointmentDateTime = a.Slot.StartDateTime,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync(cancellationToken);

            var response = new AdminDashboardSummaryResponse
            {
                TodaysAppointments = todaysAppointments,
                PendingAppointments = pendingAppointments,
                ConfirmedAppointments = confirmedAppointments,
                TotalCustomers = totalCustomers,
                StatusSummary = statusSummary,
                RecentAppointments = recentAppointments
            };

            return Ok(response);
        }

        private static TimeZoneInfo GetSouthAfricaTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "South Africa Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "Africa/Johannesburg");
            }
        }
    }
}