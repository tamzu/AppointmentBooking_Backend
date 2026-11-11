using AppointmentBooking.Core.DTOs.BranchClosures;
using AppointmentBooking.Core.DTOs.Branches;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BranchClosuresController : ControllerBase
    {
        private readonly AppointmentBookingDbContext _context;

        public BranchClosuresController(AppointmentBookingDbContext context)
        {
            _context = context;
        }

        // GET: api/branchclosures/branch/1
        [HttpGet("branch/{branchId:int}")]
        public async Task<IActionResult> GetByBranch(
            int branchId,
            CancellationToken cancellationToken)
        {
            var branchExists = await _context.Branches
                .AnyAsync(x => x.BranchId == branchId, cancellationToken);

            if (!branchExists)
                return NotFound(new { message = "Branch not found." });

            var closures = await _context.BranchClosures
                .AsNoTracking()
                .Where(x => x.BranchId == branchId)
                .OrderBy(x => x.StartDate)
                .Select(x => new BranchClosureResponse
                {
                    BranchClosureId = x.BranchClosureId,
                    BranchId = x.BranchId,
                    BranchName = x.Branch.BranchName,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    Reason = x.Reason,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return Ok(closures);
        }

        // GET: api/branchclosures/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var closure = await _context.BranchClosures
                .AsNoTracking()
                .Where(x => x.BranchClosureId == id)
                .Select(x => new BranchClosureResponse
                {
                    BranchClosureId = x.BranchClosureId,
                    BranchId = x.BranchId,
                    BranchName = x.Branch.BranchName,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    Reason = x.Reason,
                    CreatedAt = x.CreatedAt
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (closure == null)
                return NotFound(new { message = "Branch closure not found." });

            return Ok(closure);
        }

        // POST: api/branchclosures
        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateBranchClosureRequest request,
            CancellationToken cancellationToken)
        {
            var validationError = ValidateDateRange(
                request.StartDate,
                request.EndDate);

            if (validationError != null)
                return BadRequest(new { message = validationError });

            var branch = await _context.Branches
                .FirstOrDefaultAsync(
                    x => x.BranchId == request.BranchId,
                    cancellationToken);

            if (branch == null)
                return NotFound(new { message = "Branch not found." });

            // Check whether this period overlaps an existing closure.
            var overlappingClosure = await _context.BranchClosures
                .AnyAsync(x =>
                    x.BranchId == request.BranchId &&
                    x.StartDate <= request.EndDate &&
                    x.EndDate >= request.StartDate,
                    cancellationToken);

            if (overlappingClosure)
            {
                return Conflict(new
                {
                    message = "This closure overlaps an existing closure for the branch."
                });
            }

            var closure = new BranchClosure
            {
                BranchId = request.BranchId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Reason = request.Reason?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.BranchClosures.Add(closure);

            await _context.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = closure.BranchClosureId },
                new
                {
                    closure.BranchClosureId,
                    closure.BranchId,
                    closure.StartDate,
                    closure.EndDate,
                    closure.Reason,
                    closure.CreatedAt
                });
        }

        // PUT: api/branchclosures/1
        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateBranchClosureRequest request,
            CancellationToken cancellationToken)
        {
            var validationError = ValidateDateRange(
                request.StartDate,
                request.EndDate);

            if (validationError != null)
                return BadRequest(new { message = validationError });

            var closure = await _context.BranchClosures
                .FirstOrDefaultAsync(
                    x => x.BranchClosureId == id,
                    cancellationToken);

            if (closure == null)
                return NotFound(new { message = "Branch closure not found." });

            var overlappingClosure = await _context.BranchClosures
                .AnyAsync(x =>
                    x.BranchId == closure.BranchId &&
                    x.BranchClosureId != id &&
                    x.StartDate <= request.EndDate &&
                    x.EndDate >= request.StartDate,
                    cancellationToken);

            if (overlappingClosure)
            {
                return Conflict(new
                {
                    message = "This closure overlaps an existing closure for the branch."
                });
            }

            closure.StartDate = request.StartDate;
            closure.EndDate = request.EndDate;
            closure.Reason = request.Reason?.Trim();

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "Branch closure updated successfully." });
        }

        // DELETE: api/branchclosures/1
        [Authorize(Roles = "Admin,Staff")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken)
        {
            var closure = await _context.BranchClosures
                .FirstOrDefaultAsync(
                    x => x.BranchClosureId == id,
                    cancellationToken);

            if (closure == null)
                return NotFound(new { message = "Branch closure not found." });

            _context.BranchClosures.Remove(closure);

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "Branch closure deleted successfully." });
        }

        private static string? ValidateDateRange(
            DateOnly startDate,
            DateOnly endDate)
        {
            if (startDate == default || endDate == default)
                return "Start date and end date are required.";

            if (endDate < startDate)
                return "End date cannot be earlier than start date.";

            return null;
        }
    }
}
