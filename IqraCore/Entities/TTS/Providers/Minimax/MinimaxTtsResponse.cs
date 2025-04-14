using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Minimax
{
    public class MinimaxTtsResponse
    {
        [JsonPropertyName("data")]
        public MinimaxResponseData? Data { get; set; }

        [JsonPropertyName("subtitle_file")]
        public string? SubtitleFile { get; set; } // URL if subtitle_enable was true

        [JsonPropertyName("extra_info")]
        public MinimaxResponseExtraInfo? ExtraInfo { get; set; }

        [JsonPropertyName("trace_id")]
        public string? TraceId { get; set; }

        [JsonPropertyName("base_resp")]
        public MinimaxBaseResp? BaseResp { get; set; }
    }
}
