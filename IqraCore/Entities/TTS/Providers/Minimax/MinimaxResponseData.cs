using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Minimax
{
    public class MinimaxResponseData
    {
        [JsonPropertyName("data")]
        public Data Data { get; set; }

        [JsonPropertyName("extra_info")]
        public ExtraInfo ExtraInfo { get; set; }

        [JsonPropertyName("trace_id")]
        public string TraceId { get; set; }

        [JsonPropertyName("base_resp")]
        public BaseResp BaseResp { get; set; }
    }

    public class Data
    {
        [JsonPropertyName("audio")]
        public string Audio { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }
    }

    public class ExtraInfo
    {
        [JsonPropertyName("audio_length")]
        public int AudioLength { get; set; }

        [JsonPropertyName("audio_sample_rate")]
        public int AudioSampleRate { get; set; }

        [JsonPropertyName("audio_size")]
        public int AudioSize { get; set; }

        [JsonPropertyName("bitrate")]
        public int Bitrate { get; set; }

        [JsonPropertyName("word_count")]
        public int WordCount { get; set; }

        [JsonPropertyName("invisible_character_ratio")]
        public int InvisibleCharacterRatio { get; set; }

        [JsonPropertyName("usage_characters")]
        public int UsageCharacters { get; set; }

        [JsonPropertyName("audio_format")]
        public string AudioFormat { get; set; }

        [JsonPropertyName("audio_channel")]
        public int AudioChannel { get; set; }
    }

    public class BaseResp
    {
        [JsonPropertyName("status_code")]
        public int StatusCode { get; set; }

        [JsonPropertyName("status_msg")]
        public string StatusMsg { get; set; }
    }
}