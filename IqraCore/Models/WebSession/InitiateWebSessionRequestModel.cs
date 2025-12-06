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
        public InitiateWebSessionRequestAudioInputConfiguration AudioInputConfiguration { get; set; } = new InitiateWebSessionRequestAudioInputConfiguration();

        [Required]
        public InitiateWebSessionRequestAudioOutputConfiguration AudioOutputConfiguration { get; set; } = new InitiateWebSessionRequestAudioOutputConfiguration();

        public Dictionary<string, string>? DynamicVariables { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }

    public class InitiateWebSessionRequestAudioInputConfiguration
    {
        [Required]
        public int SampleRate { get; set; }

        [Required]
        public int BitsPerSample { get; set; }

        [Required]
        public AudioEncodingTypeEnum AudioEncodingType { get; set; }

        [Required]
        public AudioEncoderFallbackOptimizationMode AudioEncodingFallbackMode { get; set; }
    }

    public class InitiateWebSessionRequestAudioOutputConfiguration
    {
        [Required]
        public int SampleRate { get; set; }

        [Required]
        public int BitsPerSample { get; set; }

        [Required]
        public AudioEncodingTypeEnum AudioEncodingType { get; set; }

        [Required]
        public AudioEncoderFallbackOptimizationMode AudioEncodingFallbackMode { get; set; }

        [Required]
        public int FrameDurationMs { get; set; }

        [Required]
        public int MaxBufferAheadMs { get; set; }

        [Required]
        public int InitialSegmentDurationMs { get; set; }
    }
}
