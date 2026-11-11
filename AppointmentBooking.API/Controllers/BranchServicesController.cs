using AppointmentBooking.Core.DTOs.BranchServices;
//using AppointmentBooking.Core.DTOs.Services;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.API.Controllers
{
    //[Route("api/branches/{branchId:int}/services")]
    //[ApiController]
    [Route("api/[controller]")]
    [ApiController]
    public class BranchServicesController : ControllerBase
    {
        private readonly AppointmentBookingDbContext _context;

        public BranchServicesController(
            AppointmentBookingDbContext context)
        {
            _context = context;
        }

        // GET: api/branchservices
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBranchServices(
            CancellationToken cancellationToken)
        {
            var branchServices = await _context.BranchServices
                .AsNoTracking()
                .Select(x => new BranchServiceResponse
                {
                    BranchServiceId = x.BranchServiceId,

                    BranchId = x.BranchId,
                    BranchCode = x.Branch.BranchCode,
                    BranchName = x.Branch.BranchName,

                    ServiceId = x.ServiceId,
                    ServiceCode = x.Service.ServiceCode,
                    ServiceName = x.Service.ServiceName,

                    DurationMinutes = x.Service.DurationMinutes,

                    IsAvailable = x.IsAvailable
                })
                .OrderBy(x => x.BranchName)
                .ThenBy(x => x.ServiceName)
                .ToListAsync(cancellationToken);

            return Ok(branchServices);
        }

        // GET: api/branchservices/branch/1
        [HttpGet("branch/{branchId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetServicesByBranch(
            int branchId,
            CancellationToken cancellationToken)
        {
            var branchExists = await _context.Branches
                .AnyAsync(
                    x => x.BranchId == branchId,
                    cancellationToken);

            if (!branchExists)
                return NotFound(new { message = "Branch not found." });

            var services = await _context.BranchServices
                .AsNoTracking()
                .Where(x => x.BranchId == branchId)
                .Select(x => new BranchServiceResponse
                {
                    BranchServiceId = x.BranchServiceId,

                    BranchId = x.BranchId,
                    BranchCode = x.Branch.BranchCode,
                    BranchName = x.Branch.BranchName,

                    ServiceId = x.ServiceId,
                    ServiceCode = x.Service.ServiceCode,
                    ServiceName = x.Service.ServiceName,

                    DurationMinutes = x.Service.DurationMinutes,

                    IsAvailable = x.IsAvailable
                })
                .OrderBy(x => x.ServiceName)
                .ToListAsync(cancellationToken);

            return Ok(services);
        }

        // POST: api/branchservices
        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        [ProducesResponseType(
            typeof(BranchService),
            StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AssignServiceToBranch(
            [FromBody] CreateBranchServiceRequest request,
            CancellationToken cancellationToken)
        {
            var branch = await _context.Branches
                .FirstOrDefaultAsync(
                    x => x.BranchId == request.BranchId,
                    cancellationToken);

            if (branch == null)
                return NotFound(new { message = "Branch not found." });

            if (!branch.IsActive)
            {
                return BadRequest(new
                {
                    message = "Cannot assign a service to an inactive branch."
                });
            }

            var service = await _context.Services
                .FirstOrDefaultAsync(
                    x => x.ServiceId == request.ServiceId,
                    cancellationToken);

            if (service == null)
                return NotFound(new { message = "Service not found." });

            if (!service.IsActive)
            {
                return BadRequest(new
                {
                    message = "Cannot assign an inactive service."
                });
            }

            var alreadyAssigned = await _context.BranchServices
                .AnyAsync(
                    x => x.BranchId == request.BranchId &&
                         x.ServiceId == request.ServiceId,
                    cancellationToken);

            if (alreadyAssigned)
            {
                return Conflict(new
                {
                    message = "This service is already assigned to the branch."
                });
            }

            var branchService = new BranchService
            {
                BranchId = request.BranchId,
                ServiceId = request.ServiceId,
                IsAvailable = true
            };

            _context.BranchServices.Add(branchService);

            await _context.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(
                nameof(GetServicesByBranch),
                new { branchId = branchService.BranchId },
                new
                {
                    message = "Service successfully assigned to branch.",
                    branchServiceId = branchService.BranchServiceId,
                    branchService.BranchId,
                    branchService.ServiceId,
                    branchService.IsAvailable
                });
        }

        // PUT: api/branchservices/1
        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateBranchService(
            int id,
            [FromBody] UpdateBranchServiceRequest request,
            CancellationToken cancellationToken)
        {
            var branchService = await _context.BranchServices
                .FirstOrDefaultAsync(
                    x => x.BranchServiceId == id,
                    cancellationToken);

            if (branchService == null)
                return NotFound(new
                {
                    message = "Branch service assignment not found."
                });

            branchService.IsAvailable = request.IsAvailable;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                message = "Branch service updated successfully.",
                branchService.BranchServiceId,
                branchService.BranchId,
                branchService.ServiceId,
                branchService.IsAvailable
            });
        }

        // DELETE: api/branchservices/1
        // Soft delete: make the service unavailable at the branch.
        [Authorize(Roles = "Admin,Staff")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteBranchService(
            int id,
            CancellationToken cancellationToken)
        {
            var branchService = await _context.BranchServices
                .FirstOrDefaultAsync(
                    x => x.BranchServiceId == id,
                    cancellationToken);

            if (branchService == null)
                return NotFound(new
                {
                    message = "Branch service assignment not found."
                });

            if (!branchService.IsAvailable)
            {
                return BadRequest(new
                {
                    message = "This service is already unavailable at the branch."
                });
            }

            branchService.IsAvailable = false;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                message = "Branch service deactivated successfully.",
                branchServiceId = branchService.BranchServiceId
            });
        }
    }
}