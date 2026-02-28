using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Minimax
{
    public class MinimaxVoiceModify
    {
        [JsonPropertyName("pitch")] public int? Pitch { get; set; }
        [JsonPropertyName("intensity")] public int? Intensity { get; set; }
        [JsonPropertyName("timbre")] public int? Timbre { get; set; }
        [JsonPropertyName("sound_effects")] public string? SoundEffects { get; set; }
    }
}
