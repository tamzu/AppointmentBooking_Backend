using AppointmentBooking.Core.DTOs.Services;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServicesController : ControllerBase
    {
        private readonly AppointmentBookingDbContext _context;

        public ServicesController(AppointmentBookingDbContext context)
        {
            _context = context;
        }

        // GET: api/services
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetServices(
            CancellationToken cancellationToken)
        {
            var services = await _context.Services
                .AsNoTracking()
                .OrderBy(x => x.ServiceName)
                .ToListAsync(cancellationToken);

            return Ok(services);
        }

        // GET: api/services/1
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(Service), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetServiceById(
            int id,
            CancellationToken cancellationToken)
        {
            var service = await _context.Services
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.ServiceId == id,
                    cancellationToken);

            if (service == null)
                return NotFound(new { message = "Service not found." });

            return Ok(service);
        }

        // POST: api/services
        [HttpPost]
        [ProducesResponseType(typeof(Service), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateService(
            [FromBody] CreateServiceRequest request,
            CancellationToken cancellationToken)
        {
            var serviceCode = request.ServiceCode.Trim();

            if (string.IsNullOrWhiteSpace(serviceCode))
            {
                return BadRequest(new
                {
                    message = "Service code is required."
                });
            }

            var exists = await _context.Services
                .AnyAsync(
                    x => x.ServiceCode == serviceCode,
                    cancellationToken);

            if (exists)
            {
                return Conflict(new
                {
                    message = $"Service code '{serviceCode}' already exists."
                });
            }

            var service = new Service
            {
                ServiceCode = serviceCode,
                ServiceName = request.ServiceName.Trim(),
                Description = request.Description?.Trim(),
                DurationMinutes = request.DurationMinutes,
                IsActive = true
            };

            _context.Services.Add(service);

            await _context.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(
                nameof(GetServiceById),
                new { id = service.ServiceId },
                service);
        }

        // PUT: api/services/1
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(Service), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateService(
            int id,
            [FromBody] UpdateServiceRequest request,
            CancellationToken cancellationToken)
        {
            var service = await _context.Services
                .FirstOrDefaultAsync(
                    x => x.ServiceId == id,
                    cancellationToken);

            if (service == null)
                return NotFound(new { message = "Service not found." });

            var serviceCode = request.ServiceCode.Trim();

            var duplicateCode = await _context.Services
                .AnyAsync(
                    x => x.ServiceId != id &&
                         x.ServiceCode == serviceCode,
                    cancellationToken);

            if (duplicateCode)
            {
                return Conflict(new
                {
                    message = $"Service code '{serviceCode}' already exists."
                });
            }

            service.ServiceCode = serviceCode;
            service.ServiceName = request.ServiceName.Trim();
            service.Description = request.Description?.Trim();
            service.DurationMinutes = request.DurationMinutes;
            service.IsActive = request.IsActive;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(service);
        }

        // DELETE: api/services/1
        // Soft delete: deactivate the service instead of removing it.
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteService(
            int id,
            CancellationToken cancellationToken)
        {
            var service = await _context.Services
                .FirstOrDefaultAsync(
                    x => x.ServiceId == id,
                    cancellationToken);

            if (service == null)
                return NotFound(new { message = "Service not found." });

            if (!service.IsActive)
            {
                return BadRequest(new
                {
                    message = "Service is already inactive."
                });
            }

            service.IsActive = false;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                message = "Service deactivated successfully.",
                serviceId = service.ServiceId
            });
        }
    }
}
