using AppointmentBooking.Core.DTOs.Branches;
using AppointmentBooking.Core.DTOs.BranchOperatingHours;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BranchOperatingHoursController : ControllerBase
    {
        private readonly AppointmentBookingDbContext _context;

        public BranchOperatingHoursController(
            AppointmentBookingDbContext context)
        {
            _context = context;
        }

        // GET: api/branchoperatinghours/branch/1
        [HttpGet("branch/{branchId:int}")]
        public async Task<IActionResult> GetByBranch(
            int branchId,
            CancellationToken cancellationToken)
        {
            var branchExists = await _context.Branches
                .AnyAsync(x => x.BranchId == branchId, cancellationToken);

            if (!branchExists)
                return NotFound(new { message = "Branch not found." });

            var hours = await _context.BranchOperatingHours
                .AsNoTracking()
                .Where(x => x.BranchId == branchId)
                .OrderBy(x => x.DayOfWeek)
                .Select(x => new BranchOperatingHourResponse
                {
                    OperatingHourId = x.OperatingHourId,
                    BranchId = x.BranchId,
                    BranchName = x.Branch.BranchName,
                    DayOfWeek = x.DayOfWeek,
                    DayName = GetDayName(x.DayOfWeek),
                    OpeningTime = x.OpeningTime,
                    ClosingTime = x.ClosingTime,
                    IsClosed = x.IsClosed
                })
                .ToListAsync(cancellationToken);

            return Ok(hours);
        }

        // GET: api/branchoperatinghours/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var hour = await _context.BranchOperatingHours
                .AsNoTracking()
                .Where(x => x.OperatingHourId == id)
                .Select(x => new BranchOperatingHourResponse
                {
                    OperatingHourId = x.OperatingHourId,
                    BranchId = x.BranchId,
                    BranchName = x.Branch.BranchName,
                    DayOfWeek = x.DayOfWeek,
                    DayName = GetDayName(x.DayOfWeek),
                    OpeningTime = x.OpeningTime,
                    ClosingTime = x.ClosingTime,
                    IsClosed = x.IsClosed
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (hour == null)
                return NotFound(new { message = "Operating hours not found." });

            return Ok(hour);
        }

        // POST: api/branchoperatinghours
        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateBranchOperatingHourRequest request,
            CancellationToken cancellationToken)
        {
            var validationError = ValidateHours(
                request.IsClosed,
                request.OpeningTime,
                request.ClosingTime);

            if (validationError != null)
                return BadRequest(new { message = validationError });

            var branch = await _context.Branches
                .FirstOrDefaultAsync(
                    x => x.BranchId == request.BranchId,
                    cancellationToken);

            if (branch == null)
                return NotFound(new { message = "Branch not found." });

            if (!branch.IsActive)
                return BadRequest(new { message = "Cannot add operating hours to an inactive branch." });

            var alreadyExists = await _context.BranchOperatingHours
                .AnyAsync(
                    x => x.BranchId == request.BranchId &&
                         x.DayOfWeek == request.DayOfWeek,
                    cancellationToken);

            if (alreadyExists)
                return Conflict(new { message = "Operating hours already exist for this branch and day." });

            var operatingHour = new BranchOperatingHour
            {
                BranchId = request.BranchId,
                DayOfWeek = request.DayOfWeek,
                OpeningTime = request.IsClosed ? null : request.OpeningTime,
                ClosingTime = request.IsClosed ? null : request.ClosingTime,
                IsClosed = request.IsClosed
            };

            _context.BranchOperatingHours.Add(operatingHour);
            await _context.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = operatingHour.OperatingHourId },
                new
                {
                    operatingHour.OperatingHourId,
                    operatingHour.BranchId,
                    operatingHour.DayOfWeek,
                    operatingHour.OpeningTime,
                    operatingHour.ClosingTime,
                    operatingHour.IsClosed
                });
        }

        // PUT: api/branchoperatinghours/5
        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateBranchOperatingHourRequest request,
            CancellationToken cancellationToken)
        {
            var validationError = ValidateHours(
                request.IsClosed,
                request.OpeningTime,
                request.ClosingTime);

            if (validationError != null)
                return BadRequest(new { message = validationError });

            var operatingHour = await _context.BranchOperatingHours
                .FirstOrDefaultAsync(
                    x => x.OperatingHourId == id,
                    cancellationToken);

            if (operatingHour == null)
                return NotFound(new { message = "Operating hours not found." });

            var duplicateDay = await _context.BranchOperatingHours
                .AnyAsync(
                    x => x.BranchId == operatingHour.BranchId &&
                         x.DayOfWeek == request.DayOfWeek &&
                         x.OperatingHourId != id,
                    cancellationToken);

            if (duplicateDay)
                return Conflict(new { message = "Operating hours already exist for this branch and day." });

            operatingHour.DayOfWeek = request.DayOfWeek;
            operatingHour.OpeningTime = request.IsClosed ? null : request.OpeningTime;
            operatingHour.ClosingTime = request.IsClosed ? null : request.ClosingTime;
            operatingHour.IsClosed = request.IsClosed;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "Operating hours updated successfully." });
        }

        // DELETE: api/branchoperatinghours/5
        [Authorize(Roles = "Admin,Staff")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken)
        {
            var operatingHour = await _context.BranchOperatingHours
                .FirstOrDefaultAsync(
                    x => x.OperatingHourId == id,
                    cancellationToken);

            if (operatingHour == null)
                return NotFound(new { message = "Operating hours not found." });

            _context.BranchOperatingHours.Remove(operatingHour);
            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "Operating hours deleted successfully." });
        }

        private static string? ValidateHours(
            bool isClosed,
            TimeOnly? openingTime,
            TimeOnly? closingTime)
        {
            if (isClosed)
            {
                if (openingTime.HasValue || closingTime.HasValue)
                    return "Opening and closing times must be empty when the branch is closed.";

                return null;
            }

            if (!openingTime.HasValue || !closingTime.HasValue)
                return "Opening and closing times are required when the branch is open.";

            if (openingTime >= closingTime)
                return "Closing time must be later than opening time.";

            return null;
        }

        private static string GetDayName(byte dayOfWeek)
        {
            return dayOfWeek <= 6
                ? Enum.GetName(typeof(DayOfWeek), (DayOfWeek)dayOfWeek) ?? "Unknown"
                : "Unknown";
        }
    }
}
