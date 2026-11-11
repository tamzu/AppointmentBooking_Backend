using AppointmentBooking.API.DTOs.CustomerIdentityVerifications;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerIdentityVerificationsController : ControllerBase
    {
        private readonly AppointmentBookingDbContext _context;

        public CustomerIdentityVerificationsController(
            AppointmentBookingDbContext context)
        {
            _context = context;
        }

        // POST: api/customeridentityverifications
        [HttpPost]
        [ProducesResponseType(typeof(CustomerIdentityVerificationResponse),
            StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CustomerIdentityVerificationResponse>>
            CreateVerification(
                CreateCustomerIdentityVerificationRequest request)
        {
            var document = await _context.CustomerIdentityDocuments
                .Include(x => x.IdentityDocumentType)
                .Include(x => x.Customer)
                .FirstOrDefaultAsync(x =>
                    x.IdentityDocumentId == request.CustomerIdentityDocumentId &&
                    x.IsActive &&
                    x.Customer.IsActive);

            if (document == null)
            {
                return NotFound(new
                {
                    message = "Active customer identity document was not found."
                });
            }

            var statusCode = request.StatusCode.Trim().ToUpperInvariant();

            var status = await _context.IdentityVerificationStatuses
                .FirstOrDefaultAsync(x =>
                    x.Code == statusCode &&
                    x.IsActive);

            if (status == null)
            {
                return BadRequest(new
                {
                    message = "The specified identity verification status is invalid."
                });
            }

            var verificationMethod = request.VerificationMethod.Trim();

            if (string.IsNullOrWhiteSpace(verificationMethod))
            {
                return BadRequest(new
                {
                    message = "Verification method is required."
                });
            }

            if (statusCode == "REJECTED" &&
                string.IsNullOrWhiteSpace(request.FailureReason))
            {
                return BadRequest(new
                {
                    message = "A failure reason is required when verification is rejected."
                });
            }

            if (statusCode == "VERIFIED" &&
                !string.IsNullOrWhiteSpace(request.FailureReason))
            {
                return BadRequest(new
                {
                    message = "A failure reason cannot be supplied for a verified document."
                });
            }

            var verification = new CustomerIdentityVerification
            {
                CustomerIdentityDocumentId = document.IdentityDocumentId,
                IdentityVerificationStatusId =
                    status.IdentityVerificationStatusId,
                VerificationMethod = verificationMethod,
                ProviderReference = string.IsNullOrWhiteSpace(request.ProviderReference)
                    ? null
                    : request.ProviderReference.Trim(),
                VerifiedAt = statusCode == "VERIFIED"
                    ? DateTime.UtcNow
                    : null,
                FailureReason = string.IsNullOrWhiteSpace(request.FailureReason)
                    ? null
                    : request.FailureReason.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.CustomerIdentityVerifications.Add(verification);

            await _context.SaveChangesAsync();

            var response = MapResponse(verification, document, status);

            return CreatedAtAction(
                nameof(GetVerification),
                new { id = verification.CustomerIdentityVerificationId },
                response);
        }

        // GET: api/customeridentityverifications/{id}
        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(CustomerIdentityVerificationResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CustomerIdentityVerificationResponse>>
            GetVerification(long id)
        {
            var verification = await _context.CustomerIdentityVerifications
                .AsNoTracking()
                .Include(x => x.CustomerIdentityDocument)
                    .ThenInclude(x => x.IdentityDocumentType)
                .Include(x => x.IdentityVerificationStatus)
                .FirstOrDefaultAsync(x =>
                    x.CustomerIdentityVerificationId == id);

            if (verification == null)
            {
                return NotFound(new
                {
                    message = "Identity verification record was not found."
                });
            }

            return Ok(MapResponse(
                verification,
                verification.CustomerIdentityDocument,
                verification.IdentityVerificationStatus));
        }

        // GET: api/customeridentityverifications/customer/{customerId}
        [HttpGet("customer/{customerId:int}")]
        [ProducesResponseType(typeof(
            IEnumerable<CustomerIdentityVerificationResponse>),
            StatusCodes.Status200OK)]
        public async Task<ActionResult<
            IEnumerable<CustomerIdentityVerificationResponse>>>
            GetCustomerVerificationHistory(int customerId)
        {
            var customerExists = await _context.Customers
                .AnyAsync(x => x.CustomerId == customerId);

            if (!customerExists)
            {
                return NotFound(new
                {
                    message = "Customer was not found."
                });
            }

            var verifications = await _context.CustomerIdentityVerifications
                .AsNoTracking()
                .Where(x =>
                    x.CustomerIdentityDocument.CustomerId == customerId)
                .Include(x => x.CustomerIdentityDocument)
                    .ThenInclude(x => x.IdentityDocumentType)
                .Include(x => x.IdentityVerificationStatus)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var response = verifications.Select(x =>
                MapResponse(
                    x,
                    x.CustomerIdentityDocument,
                    x.IdentityVerificationStatus));

            return Ok(response);
        }

        private static CustomerIdentityVerificationResponse MapResponse(
            CustomerIdentityVerification verification,
            CustomerIdentityDocument document,
            IdentityVerificationStatus status)
        {
            return new CustomerIdentityVerificationResponse
            {
                CustomerIdentityVerificationId =
                    verification.CustomerIdentityVerificationId,

                CustomerIdentityDocumentId =
                    verification.CustomerIdentityDocumentId,

                MaskedDocumentNumber =
                    document.MaskedDocumentNumber,

                DocumentTypeCode =
                    document.IdentityDocumentType.Code,

                DocumentTypeName =
                    document.IdentityDocumentType.Name,

                StatusCode = status.Code,

                StatusName = status.Name,

                VerificationMethod =
                    verification.VerificationMethod,

                ProviderReference =
                    verification.ProviderReference,

                VerifiedAt =
                    verification.VerifiedAt,

                FailureReason =
                    verification.FailureReason,

                CreatedAt =
                    verification.CreatedAt
            };
        }
    }
}
