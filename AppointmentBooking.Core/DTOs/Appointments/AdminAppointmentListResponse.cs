namespace AppointmentBooking.Core.DTOs.Appointments
{
    public class AdminAppointmentListResponse
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

    public class PagedResponse<T>
    {
        public IEnumerable<T> Items { get; set; } = [];

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalRecords { get; set; }

        public int TotalPages { get; set; }
    }
}