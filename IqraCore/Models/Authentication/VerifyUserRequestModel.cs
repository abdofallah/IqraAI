using System.ComponentModel.DataAnnotations;

namespace IqraCore.Models.Authentication
{
    public class VerifyUserRequestModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Token { get; set; }
    }
}
