using AppointmentBooking.API.Security;
using AppointmentBooking.Core.DTOs.CustomerIdentityDocuments;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerIdentityDocumentsController : ControllerBase
    {
        private readonly AppointmentBookingDbContext _context;
        private readonly IIdentityDocumentSecurityService _securityService;

        public CustomerIdentityDocumentsController(
            AppointmentBookingDbContext context,
            IIdentityDocumentSecurityService securityService)
        {
            _context = context;
            _securityService = securityService;
        }

        // POST: api/customeridentitydocuments
        [HttpPost]
        [ProducesResponseType(typeof(CustomerIdentityDocumentResponse),
            StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<CustomerIdentityDocumentResponse>>
            CreateCustomerIdentityDocument(
                [FromBody] CreateCustomerIdentityDocumentRequest request)
        {
            // 1. Validate customer
            var customerExists = await _context.Customers
                .AnyAsync(x =>
                    x.CustomerId == request.CustomerId &&
                    x.IsActive);

            if (!customerExists)
            {
                return NotFound("Active customer was not found.");
            }

            // 2. Normalize document type and country
            var documentTypeCode =
                request.DocumentTypeCode.Trim().ToUpperInvariant();

            var issuingCountryCode =
                request.IssuingCountryCode.Trim().ToUpperInvariant();

            if (!System.Text.RegularExpressions.Regex.IsMatch(
                    issuingCountryCode, @"^[A-Z]{2}$"))
            {
                return BadRequest(
                    "Issuing country code must contain exactly two letters.");
            }

            // 3. Find active identity document type
            var documentType = await _context.IdentityDocumentTypes
                .FirstOrDefaultAsync(x =>
                    x.Code == documentTypeCode &&
                    x.IsActive);

            if (documentType == null)
            {
                return BadRequest("Invalid or inactive document type.");
            }

            // 4. Normalize and validate the document number
            string normalizedNumber;

            try
            {
                normalizedNumber =
                    _securityService.NormalizeDocumentNumber(
                        request.DocumentNumber,
                        documentTypeCode);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

            // 5. Encrypt and hash the identity number
            var encryptedNumber =
                _securityService.Encrypt(normalizedNumber);

            var documentHash =
                _securityService.ComputeHash(
                    normalizedNumber,
                    documentTypeCode,
                    issuingCountryCode);

            var maskedNumber =
                _securityService.MaskDocumentNumber(normalizedNumber);

            // 6. Check for duplicate identity document
            var duplicateExists = await _context.CustomerIdentityDocuments
                .AnyAsync(x =>
                    x.IdentityDocumentTypeId ==
                        documentType.IdentityDocumentTypeId &&
                    x.IssuingCountryCode == issuingCountryCode &&
                    x.DocumentNumberHash == documentHash);

            if (duplicateExists)
            {
                return Conflict(
                    "This identity document is already registered.");
            }

            // 7. Create entity
            var identityDocument = new CustomerIdentityDocument
            {
                CustomerId = request.CustomerId,

                IdentityDocumentTypeId =
                    documentType.IdentityDocumentTypeId,

                DocumentNumberEncrypted = encryptedNumber,

                DocumentNumberHash = documentHash,

                MaskedDocumentNumber = maskedNumber,

                IssuingCountryCode = issuingCountryCode,

                EncryptionKeyVersion =
                    _securityService.EncryptionKeyVersion,

                CreatedAt = DateTime.UtcNow,

                IsActive = true
            };

            _context.CustomerIdentityDocuments.Add(identityDocument);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Handle duplicate submissions that race between
                // the duplicate check and database insert.
                if (ex.InnerException is SqlException sqlException &&
                    (sqlException.Number == 2601 ||
                     sqlException.Number == 2627))
                {
                    return Conflict(
                        "This identity document is already registered.");
                }

                throw;
            }

            // 8. Return safe response
            var response = new CustomerIdentityDocumentResponse
            {
                IdentityDocumentId =
                    identityDocument.IdentityDocumentId,

                CustomerId = identityDocument.CustomerId,

                DocumentTypeCode = documentType.Code,

                DocumentTypeName = documentType.Name,

                MaskedDocumentNumber =
                    identityDocument.MaskedDocumentNumber,

                IssuingCountryCode =
                    identityDocument.IssuingCountryCode,

                CreatedAt = identityDocument.CreatedAt,

                IsActive = identityDocument.IsActive
            };

            return CreatedAtAction(
                nameof(GetCustomerIdentityDocument),
                new { id = identityDocument.IdentityDocumentId },
                response);
        }

        // GET: api/customeridentitydocuments/{id}
        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(CustomerIdentityDocumentResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CustomerIdentityDocumentResponse>>
            GetCustomerIdentityDocument(long id)
        {
            var document = await _context.CustomerIdentityDocuments
                .AsNoTracking()
                .Include(x => x.IdentityDocumentType)
                .FirstOrDefaultAsync(x =>
                    x.IdentityDocumentId == id &&
                    x.IsActive);

            if (document == null)
            {
                return NotFound("Identity document was not found.");
            }

            return Ok(MapToResponse(document));
        }

        // GET: api/customeridentitydocuments/customer/{customerId}
        [HttpGet("customer/{customerId:int}")]
        [ProducesResponseType(typeof(
            IEnumerable<CustomerIdentityDocumentResponse>),
            StatusCodes.Status200OK)]
        public async Task<ActionResult<
            IEnumerable<CustomerIdentityDocumentResponse>>>
            GetCustomerIdentityDocuments(int customerId)
        {
            var documents = await _context.CustomerIdentityDocuments
                .AsNoTracking()
                .Include(x => x.IdentityDocumentType)
                .Where(x =>
                    x.CustomerId == customerId &&
                    x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var response = documents.Select(MapToResponse).ToList();

            return Ok(response);
        }

        private static CustomerIdentityDocumentResponse MapToResponse(
            CustomerIdentityDocument document)
        {
            return new CustomerIdentityDocumentResponse
            {
                IdentityDocumentId = document.IdentityDocumentId,

                CustomerId = document.CustomerId,

                DocumentTypeCode = document.IdentityDocumentType.Code,

                DocumentTypeName = document.IdentityDocumentType.Name,

                MaskedDocumentNumber = document.MaskedDocumentNumber,

                IssuingCountryCode = document.IssuingCountryCode,

                CreatedAt = document.CreatedAt,

                IsActive = document.IsActive
            };
        }
    }
}
