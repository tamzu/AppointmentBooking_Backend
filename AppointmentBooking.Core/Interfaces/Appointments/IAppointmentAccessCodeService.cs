namespace AppointmentBooking.Core.Interfaces.Appointments
{
    public interface IAppointmentAccessCodeService
    {
        Task RequestAccessCodeAsync(
            Guid appointmentReference,
            string email,
            CancellationToken cancellationToken = default);

        Task<bool> VerifyAccessCodeAsync(
            Guid appointmentReference,
            string email,
            string code,
            CancellationToken cancellationToken = default);
    }
}