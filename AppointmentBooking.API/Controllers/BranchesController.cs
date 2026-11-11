using AppointmentBooking.Core.DTOs;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BranchesController : ControllerBase
    {
        private readonly AppointmentBookingDbContext _context;

        public BranchesController(AppointmentBookingDbContext context)
        {
            _context = context;
        }

        // GET: api/branches
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBranches(
            CancellationToken cancellationToken)
        {
            var branches = await _context.Branches
                .AsNoTracking()
                .OrderBy(x => x.BranchName)
                .ToListAsync(cancellationToken);

            return Ok(branches);
        }

        // GET: api/branches/1
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(Branch), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBranchById(
            int id,
            CancellationToken cancellationToken)
        {
            var branch = await _context.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.BranchId == id,
                    cancellationToken);

            if (branch == null)
                return NotFound(new { message = "Branch not found." });

            return Ok(branch);
        }

        // POST: api/branches
        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        [ProducesResponseType(typeof(Branch), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateBranch(
            [FromBody] CreateBranchRequest request,
            CancellationToken cancellationToken)
        {
            var branchCode = request.BranchCode.Trim();

            var exists = await _context.Branches
                .AnyAsync(
                    x => x.BranchCode == branchCode,
                    cancellationToken);

            if (exists)
            {
                return Conflict(new
                {
                    message = $"Branch code '{branchCode}' already exists."
                });
            }

            var branch = new Branch
            {
                BranchCode = branchCode,
                BranchName = request.BranchName.Trim(),
                AddressLine1 = request.AddressLine1.Trim(),
                AddressLine2 = request.AddressLine2?.Trim(),
                City = request.City.Trim(),
                Province = request.Province.Trim(),
                PostalCode = request.PostalCode?.Trim(),
                CountryCode = request.CountryCode.Trim().ToUpperInvariant(),
                PhoneNumber = request.PhoneNumber?.Trim(),
                Email = request.Email?.Trim(),
                TimeZoneId = request.TimeZoneId.Trim(),
                IsActive = request.IsActive
            };

            _context.Branches.Add(branch);

            await _context.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(
                nameof(GetBranchById),
                new { id = branch.BranchId },
                branch);
        }

        // PUT: api/branches/1
        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(Branch), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateBranch(
            int id,
            [FromBody] CreateBranchRequest request,
            CancellationToken cancellationToken)
        {
            var branch = await _context.Branches
                .FirstOrDefaultAsync(
                    x => x.BranchId == id,
                    cancellationToken);

            if (branch == null)
                return NotFound(new { message = "Branch not found." });

            var branchCode = request.BranchCode.Trim();

            var duplicateCode = await _context.Branches
                .AnyAsync(
                    x => x.BranchId != id &&
                         x.BranchCode == branchCode,
                    cancellationToken);

            if (duplicateCode)
            {
                return Conflict(new
                {
                    message = $"Branch code '{branchCode}' already exists."
                });
            }

            branch.BranchCode = branchCode;
            branch.BranchName = request.BranchName.Trim();
            branch.AddressLine1 = request.AddressLine1.Trim();
            branch.AddressLine2 = request.AddressLine2?.Trim();
            branch.City = request.City.Trim();
            branch.Province = request.Province.Trim();
            branch.PostalCode = request.PostalCode?.Trim();
            branch.CountryCode = request.CountryCode.Trim().ToUpperInvariant();
            branch.PhoneNumber = request.PhoneNumber?.Trim();
            branch.Email = request.Email?.Trim();
            branch.TimeZoneId = request.TimeZoneId.Trim();
            branch.IsActive = request.IsActive;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(branch);
        }

        // DELETE: api/branches/1
        // Soft delete: deactivate the branch instead of removing it.
        [Authorize(Roles = "Admin,Staff")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteBranch(
            int id,
            CancellationToken cancellationToken)
        {
            var branch = await _context.Branches
                .FirstOrDefaultAsync(
                    x => x.BranchId == id,
                    cancellationToken);

            if (branch == null)
                return NotFound(new { message = "Branch not found." });

            if (!branch.IsActive)
            {
                return BadRequest(new
                {
                    message = "Branch is already inactive."
                });
            }

            branch.IsActive = false;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                message = "Branch deactivated successfully.",
                branchId = branch.BranchId
            });
        }
    }
}