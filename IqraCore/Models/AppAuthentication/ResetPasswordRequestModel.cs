using System.ComponentModel.DataAnnotations;

namespace IqraCore.Models.AppAuthentication
{
    public class ResetPasswordRequestModel
    {
        [Required]
        public string Email { get; set; }
    }
}
