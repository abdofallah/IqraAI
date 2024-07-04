using System.ComponentModel.DataAnnotations;

namespace IqraCore.Models.AppAuthentication
{
    public class LoginModel
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
