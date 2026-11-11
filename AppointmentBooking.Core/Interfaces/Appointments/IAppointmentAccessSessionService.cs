namespace AppointmentBooking.Core.Interfaces.Appointments
{
    public interface IAppointmentAccessSessionService
    {
        Task<string?> CreateSessionAsync(
            Guid appointmentReference,
            string email,
            CancellationToken cancellationToken = default);

        Task<long?> ValidateSessionAsync(
            Guid appointmentReference,
            string token,
            CancellationToken cancellationToken = default);

        Task RevokeSessionsAsync(
            long appointmentId,
            CancellationToken cancellationToken = default);
    }
}