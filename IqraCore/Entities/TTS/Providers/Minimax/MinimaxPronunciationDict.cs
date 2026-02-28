using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Minimax
{
    public class MinimaxPronunciationDict
    {
        [JsonPropertyName("tone")] public List<string> Tone { get; set; }
    }
}
