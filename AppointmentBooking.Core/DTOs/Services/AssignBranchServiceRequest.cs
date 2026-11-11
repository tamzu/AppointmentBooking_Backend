using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Core.DTOs.Services
{
    public class AssignBranchServiceRequest
    {
        [Range(1, int.MaxValue)]
        public int ServiceId { get; set; }
    }
}
