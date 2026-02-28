using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraCore.Entities.TTS.Providers.ResembleAI;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.TTS;
using IqraInfrastructure.Helpers.Audio;
using IqraInfrastructure.Managers.TTS.Helpers;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class ResembleAITTSService : ITTSService, IDisposable
    {
        private static readonly HttpClient _httpClient = new();
        private readonly ILogger<ResembleAITTSService> _logger;
        private readonly string _apiKey;
        private readonly string _projectUuid;
        private readonly ResembleAiConfig _serviceConfig;

        // Endpoints
        private const string StreamingSynthesisUrl = "https://f.cluster.resemble.ai/stream";
        private const string AccountApiUrl = "https://app.resemble.ai/api/v2";

        // State
        private AudioRequestDetails _finalUserRequest;
        private TTSProviderAvailableAudioFormat _optimalResembleFormat;
        private ResembleOutputFormatDefinition _selectedApiFormat;
        private bool _audioConversationNeeded = false;

        public ResembleAITTSService(ILogger<ResembleAITTSService> logger, string projectUuid, string apiKey, ResembleAiConfig config)
        {
            _logger = logger;
            _apiKey = apiKey;
            _projectUuid = projectUuid;
            _serviceConfig = config;
        }

        public async Task<FunctionReturnResult> Initialize()
        {
            var result = new FunctionReturnResult();

            try
            {
                // 1. Prepare Request Details
                _finalUserRequest = new AudioRequestDetails
                {
                    RequestedEncoding = _serviceConfig.TargetEncodingType,
                    RequestedSampleRateHz = _serviceConfig.TargetSampleRate,
                    RequestedBitsPerSample = _serviceConfig.TargetBitsPerSample
                };

                // 2. Select Optimal Format
                var bestFallbackOrder = AudiEncoderFallbackSelector.GetFallbackOrder(_finalUserRequest, ResembleSupportedFormats);
                _optimalResembleFormat = bestFallbackOrder.FirstOrDefault();

                if (_optimalResembleFormat == null)
                {
                    return result.SetFailureResult(
                        "Initialize:FORMAT_NOT_SUPPORTED",
                        $"Resemble AI does not support a format compatible with: {_finalUserRequest.RequestedEncoding} @ {_finalUserRequest.RequestedSampleRateHz}Hz"
                    );
                }

                // 3. Map to API Definition
                var formatKey = (_optimalResembleFormat.Encoding, _optimalResembleFormat.SampleRateHz, _optimalResembleFormat.BitsPerSample);
                if (!FormatMap.TryGetValue(formatKey, out _selectedApiFormat))
                {
                    throw new InvalidOperationException($"Internal error: No mapping found for selected format: {formatKey}");
                }

                // 4. Determine Conversion Needs
                // Note: Resemble returns WAV (header). We need raw PCM. Conversion is needed to strip header.
                _audioConversationNeeded = true;

                // 5. Account Check
                var accountCheck = await CheckAccount();
                if (!accountCheck.Success)
                {
                    return result.SetFailureResult(accountCheck.Code, accountCheck.Message);
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("Initialize:EXCEPTION", $"Resemble init error: {ex.Message}");
            }
        }

        public async Task<FunctionReturnResult> CheckAccount()
        {
            var result = new FunctionReturnResult();
            try
            {
                // Check Billing Usage (as proxy for valid auth + functionality)
                var startDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
                var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

                using var request = new HttpRequestMessage(HttpMethod.Get, $"{AccountApiUrl}/account/billing_usage?start_date={startDate}&end_date={endDate}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                using var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return result.SetFailureResult("CheckAccount:INVALID_KEY", "Resemble API Key is invalid.");
                    }
                    return result.SetFailureResult("CheckAccount:API_ERROR", $"Resemble Account Check Failed: {response.StatusCode}");
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("CheckAccount:EXCEPTION", ex.Message);
            }
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            if (string.IsNullOrEmpty(text)) return (Array.Empty<byte>(), TimeSpan.Zero);

            try
            {
                var requestPayload = new ResembleTtsApiRequest
                {
                    VoiceUuid = _serviceConfig.VoiceUuid,
                    Data = text,
                    ProjectUuid = _projectUuid,
                    Model = _serviceConfig.Model,
                    SampleRate = _selectedApiFormat.SampleRate,
                    Precision = _selectedApiFormat.Precision,
                    UseHd = _serviceConfig.UseHd,
                    OutputFormat = "wav"
                };

                string jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });

                using var request = new HttpRequestMessage(HttpMethod.Post, StreamingSynthesisUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Resemble API Error {Code}: {Body}", response.StatusCode, errorBody);
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                byte[] sourceAudioData;
                using (var ms = new MemoryStream())
                {
                    using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    await stream.CopyToAsync(ms, cancellationToken);
                    sourceAudioData = ms.ToArray();
                }

                if (sourceAudioData.Length == 0)
                {
                    _logger.LogWarning("Resemble returned empty stream.");
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                var duration = AudioConversationHelper.CalculateDuration(sourceAudioData, _optimalResembleFormat);

                if (_audioConversationNeeded)
                {
                    var (convertedData, _) = AudioConversationHelper.Convert(sourceAudioData, _optimalResembleFormat, _finalUserRequest, false);
                    return (convertedData, duration);
                }

                return (sourceAudioData, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Resemble Synthesis Error");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        public Task StopTextSynthesisAsync() => Task.CompletedTask;

        public string GetProviderFullName() => "ResembleAITextToSpeech";
        public InterfaceTTSProviderEnum GetProviderType() => GetProviderTypeStatic();
        public static InterfaceTTSProviderEnum GetProviderTypeStatic() => InterfaceTTSProviderEnum.ResembleAITextToSpeech;
        public ITTSConfig GetCacheableConfig() => _serviceConfig;

        public TTSProviderAvailableAudioFormat GetCurrentOutputFormat() => _optimalResembleFormat;

        public void Dispose() => GC.SuppressFinalize(this);

        // =================================================================================================
        // STATIC DATA & MAPPINGS
        // =================================================================================================

        private record ResembleOutputFormatDefinition(string Precision, int SampleRate);

        private static readonly ReadOnlyCollection<TTSProviderAvailableAudioFormat> ResembleSupportedFormats;
        private static readonly ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), ResembleOutputFormatDefinition> FormatMap;

        static ResembleAITTSService()
        {
            var supportedFormats = new List<TTSProviderAvailableAudioFormat>
            {
                // 16-bit
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 8000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 16000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 22050, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 32000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 44100, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 48000, BitsPerSample = 16 },

                // 24-bit
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 8000, BitsPerSample = 24 },
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 16000, BitsPerSample = 24 },
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 22050, BitsPerSample = 24 },
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 32000, BitsPerSample = 24 },
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 44100, BitsPerSample = 24 },
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 48000, BitsPerSample = 24 },

                // 32-bit
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 8000, BitsPerSample = 32 },
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 16000, BitsPerSample = 32 },
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 22050, BitsPerSample = 32 },
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 32000, BitsPerSample = 32 },
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 44100, BitsPerSample = 32 },
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 48000, BitsPerSample = 32 }
            };
            ResembleSupportedFormats = supportedFormats.AsReadOnly();

            var formatMap = new Dictionary<(AudioEncodingTypeEnum, int, int), ResembleOutputFormatDefinition>
            {
                // 16-bit Mappings
                { (AudioEncodingTypeEnum.WAV, 8000, 16), new("PCM_16", 8000) },
                { (AudioEncodingTypeEnum.WAV, 16000, 16), new("PCM_16", 16000) },
                { (AudioEncodingTypeEnum.WAV, 22050, 16), new("PCM_16", 22050) },
                { (AudioEncodingTypeEnum.WAV, 32000, 16), new("PCM_16", 32000) },
                { (AudioEncodingTypeEnum.WAV, 44100, 16), new("PCM_16", 44100) },
                { (AudioEncodingTypeEnum.WAV, 48000, 16), new("PCM_16", 48000) },

                // 24-bit Mappings
                { (AudioEncodingTypeEnum.WAV, 8000, 24), new("PCM_24", 8000) },
                { (AudioEncodingTypeEnum.WAV, 16000, 24), new("PCM_24", 16000) },
                { (AudioEncodingTypeEnum.WAV, 22050, 24), new("PCM_24", 22050) },
                { (AudioEncodingTypeEnum.WAV, 32000, 24), new("PCM_24", 32000) },
                { (AudioEncodingTypeEnum.WAV, 44100, 24), new("PCM_24", 44100) },
                { (AudioEncodingTypeEnum.WAV, 48000, 24), new("PCM_24", 48000) },

                // 32-bit Mappings
                { (AudioEncodingTypeEnum.WAV, 8000, 32), new("PCM_32", 8000) },
                { (AudioEncodingTypeEnum.WAV, 16000, 32), new("PCM_32", 16000) },
                { (AudioEncodingTypeEnum.WAV, 22050, 32), new("PCM_32", 22050) },
                { (AudioEncodingTypeEnum.WAV, 32000, 32), new("PCM_32", 32000) },
                { (AudioEncodingTypeEnum.WAV, 44100, 32), new("PCM_32", 44100) },
                { (AudioEncodingTypeEnum.WAV, 48000, 32), new("PCM_32", 48000) },
            };
            FormatMap = new ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), ResembleOutputFormatDefinition>(formatMap);
        }
    }
}