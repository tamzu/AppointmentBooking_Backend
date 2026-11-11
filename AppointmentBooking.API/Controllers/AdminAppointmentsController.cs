
using AppointmentBooking.Core.DTOs.Appointments;
using AppointmentBooking.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.API.Controllers
{
    [Route("api/admin/appointments")]
    [ApiController]
    [Authorize(Roles = "Admin,Staff")]
    public class AdminAppointmentsController : ControllerBase
    {
        private readonly AppointmentBookingDbContext _context;

        public AdminAppointmentsController(
            AppointmentBookingDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/appointments/{appointmentId}
        [HttpGet("{appointmentId:long}")]
        [ProducesResponseType(
            typeof(AdminAppointmentDetailsResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AdminAppointmentDetailsResponse>>
            GetAppointmentDetails(
                long appointmentId,
                CancellationToken cancellationToken)
        {
            var appointment = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.AppointmentId == appointmentId)
                .Select(a => new AdminAppointmentDetailsResponse
                {
                    AppointmentId = a.AppointmentId,
                    AppointmentReference = a.AppointmentReference,

                    CustomerId = a.Customer.CustomerId,
                    CustomerName = a.Customer.FirstName + " " + a.Customer.LastName,
                    CustomerEmail = a.Customer.Email,
                    CustomerPhone = a.Customer.PhoneNumber,

                    BranchId = a.Slot.Branch.BranchId,
                    BranchName = a.Slot.Branch.BranchName,
                    BranchCode = a.Slot.Branch.BranchCode,
                    BranchAddress = a.Slot.Branch.AddressLine1,
                    BranchCity = a.Slot.Branch.City,
                    BranchProvince = a.Slot.Branch.Province,

                    ServiceId = a.Slot.Service.ServiceId,
                    ServiceName = a.Slot.Service.ServiceName,
                    ServiceDescription = a.Slot.Service.Description,
                    ServiceDurationMinutes = a.Slot.Service.DurationMinutes,

                    SlotId = a.Slot.SlotId,
                    AppointmentDateTime = a.Slot.StartDateTime,
                    AppointmentEndDateTime = a.Slot.EndDateTime,
                    BookingDate = a.BookingDate,
                    StatusCode = a.Status.Code,
                    StatusName = a.Status.Name,
                    CustomerNotes = a.CustomerNotes,
                    CancellationReason = a.CancellationReason,
                    CancelledAt = a.CancelledAt,

                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (appointment == null)
            {
                return NotFound(new
                {
                    message = $"Appointment {appointmentId} was not found."
                });
            }

            return Ok(appointment);
        }

        // GET: api/admin/appointments
        [HttpGet]
        [ProducesResponseType(
            typeof(PagedResponse<AdminAppointmentListResponse>),
            StatusCodes.Status200OK)]
        public async Task<ActionResult<
            PagedResponse<AdminAppointmentListResponse>>> GetAppointments(
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 10,
                [FromQuery] string? search = null,
                [FromQuery] string? status = null,
                [FromQuery] DateTime? dateFrom = null,
                [FromQuery] DateTime? dateTo = null,
                CancellationToken cancellationToken = default)
        {
            // Validate pagination.
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.Appointments
                .AsNoTracking()
                .AsQueryable();

            // Search customer details or appointment reference.
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim();

                query = query.Where(a =>
                    a.Customer.FirstName.Contains(searchTerm) ||
                    a.Customer.LastName.Contains(searchTerm) ||
                    (a.Customer.FirstName + " " +
                     a.Customer.LastName).Contains(searchTerm) ||
                    (a.Customer.Email != null &&
                     a.Customer.Email.Contains(searchTerm)) ||
                    a.Customer.PhoneNumber.Contains(searchTerm) ||
                    a.AppointmentReference.ToString().Contains(searchTerm));
            }

            // Filter by appointment status code.
            if (!string.IsNullOrWhiteSpace(status))
            {
                var statusCode = status.Trim().ToUpper();

                query = query.Where(a =>
                    a.Status.Code == statusCode);
            }

            // Filter by appointment date.
            if (dateFrom.HasValue)
            {
                var from = dateFrom.Value.Date;

                query = query.Where(a =>
                    a.Slot.StartDateTime >= from);
            }

            if (dateTo.HasValue)
            {
                var toExclusive = dateTo.Value.Date.AddDays(1);

                query = query.Where(a =>
                    a.Slot.StartDateTime < toExclusive);
            }

            // Get total records before pagination.
            var totalRecords = await query.CountAsync(
                cancellationToken);

            var totalPages = (int)Math.Ceiling(
                totalRecords / (double)pageSize);

            // Retrieve the requested page.
            var appointments = await query
                .OrderByDescending(a => a.Slot.StartDateTime)
                .ThenByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AdminAppointmentListResponse
                {
                    AppointmentId = a.AppointmentId,
                    AppointmentReference = a.AppointmentReference,

                    CustomerName =
                        a.Customer.FirstName + " " +
                        a.Customer.LastName,

                    CustomerEmail = a.Customer.Email,
                    CustomerPhone = a.Customer.PhoneNumber,

                    BranchName = a.Slot.Branch.BranchName,
                    ServiceName = a.Slot.Service.ServiceName,

                    StatusCode = a.Status.Code,
                    StatusName = a.Status.Name,

                    AppointmentDateTime =
                        a.Slot.StartDateTime,

                    CreatedAt = a.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return Ok(new PagedResponse<
                AdminAppointmentListResponse>
            {
                Items = appointments,
                Page = page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = totalPages
            });
        }
    }
}
