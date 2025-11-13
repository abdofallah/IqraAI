using IqraCore.Entities.Helper.Audio;
using System.ComponentModel.DataAnnotations;

namespace IqraCore.Models.WebSession
{
    public class InitiateWebSessionRequestModel
    {
        [Required]
        public string WebCampaignId { get; set; } = null!;

        [Required]
        public string RegionId { get; set; } = null!;

        [Required]
        public string ClientIdentifier { get; set; } = null!;

        [Required]
        public InitiateWebSessionRequestAudioConfiguration AudioConfiguration { get; set; } = new InitiateWebSessionRequestAudioConfiguration();

        public Dictionary<string, string>? DynamicVariables { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }

    public class InitiateWebSessionRequestAudioConfiguration
    {
        [Required]
        public int SampleRate { get; set; }

        [Required]
        public int BitsPerSample { get; set; }

        [Required]
        public AudioEncodingTypeEnum AudioEncodingType { get; set; }
    }
}
