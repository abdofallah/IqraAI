using System.ComponentModel.DataAnnotations;

namespace IqraCore.Models
{
    public class ResetPasswordRequestModel
    {
        [Required]
        public string Email { get; set; }
    }
}
