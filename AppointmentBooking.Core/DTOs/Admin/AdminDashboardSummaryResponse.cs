namespace AppointmentBooking.Core.DTOs.Admin
{
    public class AdminDashboardSummaryResponse
    {
        public int TodaysAppointments { get; set; }

        public int PendingAppointments { get; set; }

        public int ConfirmedAppointments { get; set; }

        public int TotalCustomers { get; set; }

        public List<AppointmentStatusSummaryDto> StatusSummary { get; set; }
            = new();

        public List<RecentAppointmentDto> RecentAppointments { get; set; }
            = new();
    }

    public class AppointmentStatusSummaryDto
    {
        public string StatusCode { get; set; } = string.Empty;

        public string StatusName { get; set; } = string.Empty;

        public int Count { get; set; }
    }

    public class RecentAppointmentDto
    {
        public long AppointmentId { get; set; }

        public Guid AppointmentReference { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string? CustomerEmail { get; set; }

        public string CustomerPhone { get; set; } = string.Empty;

        public string BranchName { get; set; } = string.Empty;

        public string ServiceName { get; set; } = string.Empty;

        public string StatusCode { get; set; } = string.Empty;

        public string StatusName { get; set; } = string.Empty;

        public DateTime AppointmentDateTime { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}