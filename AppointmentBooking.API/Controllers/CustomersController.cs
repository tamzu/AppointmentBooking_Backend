using AppointmentBooking.Core.DTOs.Customers;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly AppointmentBookingDbContext _context;

        public CustomersController(AppointmentBookingDbContext context)
        {
            _context = context;
        }

        // POST: api/customers
        [HttpPost]
        [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CustomerResponse>> CreateCustomer(
            [FromBody] CreateCustomerRequest request,
            CancellationToken cancellationToken)
        {
            var customer = new Customer
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = string.IsNullOrWhiteSpace(request.Email)
                    ? null
                    : request.Email.Trim(),
                PhoneNumber = request.PhoneNumber.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Customers.Add(customer);

            await _context.SaveChangesAsync(cancellationToken);

            var response = new CustomerResponse
            {
                CustomerId = customer.CustomerId,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email,
                PhoneNumber = customer.PhoneNumber,
                CreatedAt = customer.CreatedAt,
                IsActive = customer.IsActive
            };

            return CreatedAtAction(
                nameof(GetCustomer),
                new { id = customer.CustomerId },
                response);
        }

        // GET: api/customers/1
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CustomerResponse>> GetCustomer(
            int id,
            CancellationToken cancellationToken)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .Where(x => x.CustomerId == id && x.IsActive)
                .Select(x => new CustomerResponse
                {
                    CustomerId = x.CustomerId,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    Email = x.Email,
                    PhoneNumber = x.PhoneNumber,
                    CreatedAt = x.CreatedAt,
                    IsActive = x.IsActive
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (customer == null)
            {
                return NotFound(new
                {
                    message = $"Customer with ID {id} was not found."
                });
            }

            return Ok(customer);
        }
    }
    }
