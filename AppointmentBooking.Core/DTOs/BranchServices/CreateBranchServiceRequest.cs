using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Core.DTOs.BranchServices
{
    public class CreateBranchServiceRequest
    {
        [Range(1, int.MaxValue)]
        public int BranchId { get; set; }

        [Range(1, int.MaxValue)]
        public int ServiceId { get; set; }
    }
}
