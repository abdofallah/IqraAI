using System.ComponentModel.DataAnnotations;

namespace IqraCore.Models.AppAuthentication
{
    public class ResetPasswordModel
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        [StringLength(32, MinimumLength = 8)]
        public string NewPassword { get; set; }
    }
}
