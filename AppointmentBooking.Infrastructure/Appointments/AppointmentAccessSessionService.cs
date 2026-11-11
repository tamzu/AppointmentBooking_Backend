using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Interfaces.Appointments;
using AppointmentBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace AppointmentBooking.Infrastructure.Appointments
{
    public class AppointmentAccessSessionService
        : IAppointmentAccessSessionService
    {
        private static readonly TimeSpan SessionLifetime =
            TimeSpan.FromMinutes(15);

        private readonly AppointmentBookingDbContext _context;

        public AppointmentAccessSessionService(
            AppointmentBookingDbContext context)
        {
            _context = context;
        }

        public async Task<string?> CreateSessionAsync(
            Guid appointmentReference,
            string email,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            var normalizedEmail = email.Trim().ToLowerInvariant();

            // Confirm that the appointment belongs to this email address.
            var appointment = await _context.Appointments
                .Include(x => x.Customer)
                .FirstOrDefaultAsync(
                    x =>
                        x.AppointmentReference == appointmentReference &&
                        x.Customer.Email != null &&
                        x.Customer.Email.ToLower() == normalizedEmail,
                    cancellationToken);

            if (appointment == null)
                return null;

            // Generate a cryptographically secure 256-bit token.
            var randomBytes = RandomNumberGenerator.GetBytes(32);

            var token = Convert.ToBase64String(randomBytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            // Store only the SHA-256 hash.
            var tokenHash = GenerateTokenHash(token);

            var now = DateTime.UtcNow;

            var session = new AppointmentAccessSession
            {
                AppointmentId = appointment.AppointmentId,
                TokenHash = tokenHash,
                CreatedAt = now,
                ExpiresAt = now.Add(SessionLifetime)
            };

            _context.AppointmentAccessSessions.Add(session);

            await _context.SaveChangesAsync(cancellationToken);

            // Return the raw token only once.
            return token;
        }

        public async Task<long?> ValidateSessionAsync(
            Guid appointmentReference,
            string token,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var tokenHash = GenerateTokenHash(token);
            var now = DateTime.UtcNow;

            var session = await _context.AppointmentAccessSessions
                .AsNoTracking()
                .Where(
                    x =>
                        x.TokenHash == tokenHash &&
                        x.Appointment.AppointmentReference ==
                            appointmentReference &&
                        x.RevokedAt == null &&
                        x.ExpiresAt > now)
                .Select(x => new
                {
                    x.AppointmentId
                })
                .FirstOrDefaultAsync(cancellationToken);

            return session?.AppointmentId;
        }

        public async Task RevokeSessionsAsync(
            long appointmentId,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var activeSessions = await _context
                .AppointmentAccessSessions
                .Where(
                    x =>
                        x.AppointmentId == appointmentId &&
                        x.RevokedAt == null &&
                        x.ExpiresAt > now)
                .ToListAsync(cancellationToken);

            foreach (var session in activeSessions)
            {
                session.RevokedAt = now;
            }

            if (activeSessions.Count > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        private static string GenerateTokenHash(string token)
        {
            var tokenBytes = Encoding.UTF8.GetBytes(token);

            var hash = SHA256.HashData(tokenBytes);

            return Convert.ToHexString(hash);
        }
    }
}