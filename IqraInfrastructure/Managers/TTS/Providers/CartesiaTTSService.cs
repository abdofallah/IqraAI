using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraCore.Entities.TTS.Providers.Cartesia;
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
    public class CartesiaTTSService : ITTSService
    {
        private static readonly HttpClient _httpClient = new();
        private readonly ILogger<CartesiaTTSService> _logger;
        private readonly string _apiKey;
        private readonly CartesiaConfig _serviceConfig;

        // Constants
        private const string BaseUrl = "https://api.cartesia.ai";
        private const string CartesiaVersion = "2025-04-16";

        // State
        private AudioRequestDetails _finalUserRequest;
        private TTSProviderAvailableAudioFormat _optimalCartesiaFormat;
        private CartesiaOutputFormatDefinition _selectedApiFormat;
        private bool _audioConversationNeeded = false;

        public CartesiaTTSService(ILogger<CartesiaTTSService> logger, string apiKey, CartesiaConfig config)
        {
            _logger = logger;
            _apiKey = apiKey;
            _serviceConfig = config;
        }

        public async Task<FunctionReturnResult> Initialize()
        {
            var result = new FunctionReturnResult();

            try
            {
                // Prepare Request Details
                _finalUserRequest = new AudioRequestDetails
                {
                    RequestedEncoding = _serviceConfig.TargetEncodingType,
                    RequestedSampleRateHz = _serviceConfig.TargetSampleRate,
                    RequestedBitsPerSample = _serviceConfig.TargetBitsPerSample
                };

                // Select Optimal Format using the Fallback Selector
                var bestFallbackOrder = AudiEncoderFallbackSelector.GetFallbackOrder(_finalUserRequest, CartesiaSupportedFormats);
                _optimalCartesiaFormat = bestFallbackOrder.FirstOrDefault();

                if (_optimalCartesiaFormat == null)
                {
                    return result.SetFailureResult(
                        "Initialize:FORMAT_NOT_SUPPORTED",
                        $"Cartesia TTS does not support a format compatible with: {_finalUserRequest.RequestedEncoding} @ {_finalUserRequest.RequestedSampleRateHz}Hz"
                    );
                }

                // Map to Cartesia API Structure
                var formatKey = (_optimalCartesiaFormat.Encoding, _optimalCartesiaFormat.SampleRateHz, _optimalCartesiaFormat.BitsPerSample);
                if (!FormatMap.TryGetValue(formatKey, out _selectedApiFormat))
                {
                    throw new InvalidOperationException($"Internal error: No API mapping found for selected format: {formatKey}");
                }

                // Determine if Post-Processing is needed
                _audioConversationNeeded = _optimalCartesiaFormat.Encoding != _finalUserRequest.RequestedEncoding ||
                                           _optimalCartesiaFormat.SampleRateHz != _finalUserRequest.RequestedSampleRateHz ||
                                           _optimalCartesiaFormat.BitsPerSample != _finalUserRequest.RequestedBitsPerSample;

                // Account Check (Dry Run or lightweight check could go here, strictly strictly optional as per docs)
                // Since Cartesia doesn't have a specific "Check" endpoint, we assume initialization is successful if logic passes.

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Cartesia TTS Service.");
                return result.SetFailureResult("Initialize:EXCEPTION", ex.Message);
            }
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }

            try
            {
                bool isSonic3 = _serviceConfig.ModelId.Contains("sonic-3");

                var requestPayload = new
                {
                    model_id = _serviceConfig.ModelId,
                    transcript = text,
                    voice = new { mode = "id", id = _serviceConfig.VoiceId },
                    output_format = new
                    {
                        container = _selectedApiFormat.Container,
                        encoding = _selectedApiFormat.Encoding,
                        sample_rate = _selectedApiFormat.SampleRate
                    },
                    language = _serviceConfig.LanguageCode,

                    pronunciation_dict_id = isSonic3 && !string.IsNullOrWhiteSpace(_serviceConfig.PronunciationDictId) ? _serviceConfig.PronunciationDictId : null,

                    // Sonic-3 specific generation config
                    generation_config = isSonic3 ? new
                    {
                        volume = _serviceConfig.Volume,
                        speed = _serviceConfig.Speed,
                        emotion = !string.IsNullOrEmpty(_serviceConfig.Emotion) ? _serviceConfig.Emotion : null
                    } : null
                };

                var jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/tts/bytes");

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                request.Headers.Add("Cartesia-Version", CartesiaVersion);
                request.Headers.Add("X-API-Key", _apiKey); // Docs mention both, using header is safer
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Cartesia API Error {StatusCode}: {Content}", response.StatusCode, errorContent);
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                byte[] sourceAudioData = await response.Content.ReadAsByteArrayAsync(cancellationToken);

                var duration = AudioConversationHelper.CalculateDuration(sourceAudioData, _optimalCartesiaFormat);

                if (_audioConversationNeeded)
                {
                    var (convertedData, _) = AudioConversationHelper.Convert(sourceAudioData, _optimalCartesiaFormat, _finalUserRequest, false);
                    return (convertedData, duration);
                }

                return (sourceAudioData, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error synthesizing text with Cartesia.");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        public Task StopTextSynthesisAsync()
        {
            // Stateless HTTP requests are cancelled via CancellationToken in SynthesizeTextAsync
            return Task.CompletedTask;
        }

        public string GetProviderFullName() => "CartesiaTextToSpeech";
        public InterfaceTTSProviderEnum GetProviderType() => GetProviderTypeStatic();
        public static InterfaceTTSProviderEnum GetProviderTypeStatic() => InterfaceTTSProviderEnum.CartesiaTextToSpeech;

        public ITTSConfig GetCacheableConfig() => _serviceConfig;

        public TTSProviderAvailableAudioFormat GetCurrentOutputFormat() => _optimalCartesiaFormat;

        // =================================================================================================
        // STATIC DATA & MAPPINGS
        // =================================================================================================

        private record CartesiaOutputFormatDefinition(string Container, string Encoding, int SampleRate);

        private static readonly ReadOnlyCollection<TTSProviderAvailableAudioFormat> CartesiaSupportedFormats;
        private static readonly ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), CartesiaOutputFormatDefinition> FormatMap;

        static CartesiaTTSService()
        {
            var supportedFormats = new List<TTSProviderAvailableAudioFormat>
            {
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 8000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 16000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 22050, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 24000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 44100, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 48000, BitsPerSample = 16 },

                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 8000, BitsPerSample = 32 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 16000, BitsPerSample = 32 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 22050, BitsPerSample = 32 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 24000, BitsPerSample = 32 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 44100, BitsPerSample = 32 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 48000, BitsPerSample = 32 },
            };
            CartesiaSupportedFormats = supportedFormats.AsReadOnly();

            var formatMap = new Dictionary<(AudioEncodingTypeEnum, int, int), CartesiaOutputFormatDefinition>
            {
                { (AudioEncodingTypeEnum.PCM, 8000, 16), new("raw", "pcm_s16le", 8000) },
                { (AudioEncodingTypeEnum.PCM, 16000, 16), new("raw", "pcm_s16le", 16000) },
                { (AudioEncodingTypeEnum.PCM, 22050, 16), new("raw", "pcm_s16le", 22050) },
                { (AudioEncodingTypeEnum.PCM, 24000, 16), new("raw", "pcm_s16le", 24000) },
                { (AudioEncodingTypeEnum.PCM, 44100, 16), new("raw", "pcm_s16le", 44100) },
                { (AudioEncodingTypeEnum.PCM, 48000, 16), new("raw", "pcm_s16le", 48000) },

                { (AudioEncodingTypeEnum.PCM, 8000, 32), new("raw", "pcm_f32le", 8000) },
                { (AudioEncodingTypeEnum.PCM, 16000, 32), new("raw", "pcm_f32le", 16000) },
                { (AudioEncodingTypeEnum.PCM, 22050, 32), new("raw", "pcm_f32le", 22050) },
                { (AudioEncodingTypeEnum.PCM, 24000, 32), new("raw", "pcm_f32le", 24000) },
                { (AudioEncodingTypeEnum.PCM, 44100, 32), new("raw", "pcm_f32le", 44100) },
                { (AudioEncodingTypeEnum.PCM, 48000, 32), new("raw", "pcm_f32le", 48000) },
            };
            FormatMap = new ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), CartesiaOutputFormatDefinition>(formatMap);
        }
    }
}