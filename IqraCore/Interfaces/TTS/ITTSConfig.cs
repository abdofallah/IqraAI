using System.Text.Json.Serialization;

namespace IqraCore.Interfaces.TTS
{
    public class ITtsConfig
    {
        [JsonPropertyOrder(-1)]
        public int ConfigVersion { get; }
    }
}
