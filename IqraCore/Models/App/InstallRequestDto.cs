using System.ComponentModel.DataAnnotations;

namespace IqraCore.Models.App
{
    public class InstallRequestDto
    {
        // Admin Account
        [Required]
        [EmailAddress]
        public string AdminEmail { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string AdminPassword { get; set; } = string.Empty;

        // Configuration
        [Required]
        public InstallRequestDefaultS3ConfigDto S3Config { get; set; } = new InstallRequestDefaultS3ConfigDto();

        [Required]
        public bool EnableExtraTelemetry { get; set; } = true;
    }

    public class InstallRequestDefaultS3ConfigDto
    {
        [Required]
        public string Endpoint { get; set; } = string.Empty;

        [Required]
        public bool UseSSL { get; set; } = true;

        [Required]
        public string AccessKey { get; set; } = string.Empty;

        [Required]
        public string SecretKey { get; set; } = string.Empty;
    }
}