namespace AppointmentBooking.Core.DTOs.Admin
{
    public class AdminAppointmentListResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        public IEnumerable<AdminAppointmentListItemDto> Items { get; set; }
            = Array.Empty<AdminAppointmentListItemDto>();
    }

    public class AdminAppointmentListItemDto
    {
        public long AppointmentId { get; set; }
        public Guid AppointmentReference { get; set; }

        public int CustomerId { get; set; }
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