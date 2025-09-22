using System.ComponentModel.DataAnnotations;

namespace IqraCore.Models.User.Usage
{
    public class GetUserUsageCountRequestModel
    {
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public bool ComparePrevious { get; set; } = false;

        public List<long>? BusinessIds { get; set; } = null;
    }
}
