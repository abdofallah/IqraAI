using System.ComponentModel.DataAnnotations;

namespace IqraCore.Models.Authentication
{
    public class ResetPasswordRequestModel
    {
        [Required]
        public string Email { get; set; }
    }
}
