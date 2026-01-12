using System.ComponentModel.DataAnnotations;

namespace IqraCore.Models.App
{
    public class InstallRequestDto
    {
        [Required]
        [EmailAddress]
        public string AdminEmail { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string AdminPassword { get; set; } = string.Empty;

        public bool EnableExtraTelemetry { get; set; } = true;
    }
}