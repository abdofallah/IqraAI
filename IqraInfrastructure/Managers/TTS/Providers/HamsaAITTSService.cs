using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraCore.Entities.TTS.Providers.Hamsa;
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
    public class HamsaAITTSService : ITTSService, IDisposable
    {
        private static readonly HttpClient _httpClient = new();
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

        private readonly ILogger<HamsaAITTSService> _logger;
        private readonly string _apiKey;
        private const string ApiUrl = "https://api.tryhamsa.com/v1/realtime/tts";
        private readonly HamsaAiConfig _serviceConfig;

        // State
        private AudioRequestDetails _finalUserRequest;
        private HamsaAIOutputFormatDefinition _selectedApiFormat;
        private TTSProviderAvailableAudioFormat _optimalHamsaFormat;
        private bool _audioConversationNeeded = false;

        public HamsaAITTSService(ILogger<HamsaAITTSService> logger, string apiKey, HamsaAiConfig config)
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
                _finalUserRequest = new AudioRequestDetails
                {
                    RequestedEncoding = _serviceConfig.TargetEncodingType,
                    RequestedSampleRateHz = _serviceConfig.TargetSampleRate,
                    RequestedBitsPerSample = _serviceConfig.TargetBitsPerSample
                };

                var bestFallbackOrder = AudiEncoderFallbackSelector.GetFallbackOrder(_finalUserRequest, HamsaSupportedFormats);
                _optimalHamsaFormat = bestFallbackOrder.FirstOrDefault();

                if (_optimalHamsaFormat == null)
                {
                    return result.SetFailureResult(
                        "Initialize:FORMAT_NOT_SUPPORTED",
                        $"Hamsa AI TTS does not support a format compatible with: {_finalUserRequest.RequestedEncoding} @ {_finalUserRequest.RequestedSampleRateHz}Hz"
                    );
                }

                var formatKey = (_optimalHamsaFormat.Encoding, _optimalHamsaFormat.SampleRateHz, _optimalHamsaFormat.BitsPerSample);
                if (!FormatToRequestParamMap.TryGetValue(formatKey, out _selectedApiFormat))
                {
                    throw new InvalidOperationException($"Internal error: No mapping found for selected format: {formatKey}");
                }

                _audioConversationNeeded = _optimalHamsaFormat.Encoding != _finalUserRequest.RequestedEncoding ||
                                           _optimalHamsaFormat.SampleRateHz != _finalUserRequest.RequestedSampleRateHz ||
                                           _optimalHamsaFormat.BitsPerSample != _finalUserRequest.RequestedBitsPerSample;

                var accountCheck = await CheckAccount();
                if (!accountCheck.Success)
                {
                    return result.SetFailureResult(accountCheck.Code, accountCheck.Message);
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("Initialize:EXCEPTION", $"Hamsa init error: {ex.Message}");
            }
        }

        public async Task<FunctionReturnResult> CheckAccount()
        {
            // Hamsa does not currently expose an endpoint to check balance/auth.
            // We assume valid configuration until the first synthesis fails (401/403).
            return await Task.FromResult(new FunctionReturnResult().SetSuccessResult());
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            if (string.IsNullOrWhiteSpace(text)) return (Array.Empty<byte>(), TimeSpan.Zero);

            var requestPayload = new HamsaTtsApiRequest
            {
                Text = text,
                Speaker = _serviceConfig.Speaker,
                Dialect = _serviceConfig.Dialect,
                MuLaw = false
            };

            string jsonPayload = JsonSerializer.Serialize(requestPayload, _jsonSerializerOptions);

            using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Token", _apiKey);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var responseError = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Hamsa API Error {Code}: {Error}", response.StatusCode, responseError);
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                byte[] sourceAudioData = await response.Content.ReadAsByteArrayAsync(cancellationToken);

                var duration = AudioConversationHelper.CalculateDuration(sourceAudioData, _optimalHamsaFormat);

                if (_audioConversationNeeded)
                {
                    var (convertedData, _) = AudioConversationHelper.Convert(sourceAudioData, _optimalHamsaFormat, _finalUserRequest, false);
                    return (convertedData, duration);
                }
                
                return (sourceAudioData, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hamsa Synthesis Error");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        public Task StopTextSynthesisAsync() => Task.CompletedTask;

        public string GetProviderFullName() => "HamsaAITextToSpeech";
        public InterfaceTTSProviderEnum GetProviderType() => GetProviderTypeStatic();
        public static InterfaceTTSProviderEnum GetProviderTypeStatic() => InterfaceTTSProviderEnum.HamsaAITextToSpeech;
        public ITTSConfig GetCacheableConfig() => _serviceConfig;

        public TTSProviderAvailableAudioFormat GetCurrentOutputFormat() => _optimalHamsaFormat;

        public void Dispose() => GC.SuppressFinalize(this);

        // =================================================================================================
        // STATIC DATA & MAPPINGS
        // =================================================================================================

        private record HamsaAIOutputFormatDefinition(AudioEncodingTypeEnum encoding, int SampleRateHz, int BitsPerSample);

        private static readonly ReadOnlyCollection<TTSProviderAvailableAudioFormat> HamsaSupportedFormats;
        private static readonly ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), HamsaAIOutputFormatDefinition> FormatToRequestParamMap;

        static HamsaAITTSService()
        {
            var supportedFormats = new List<TTSProviderAvailableAudioFormat>
            {
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 16000, BitsPerSample = 16 }
            };
            HamsaSupportedFormats = supportedFormats.AsReadOnly();

            var formatMap = new Dictionary<(AudioEncodingTypeEnum, int, int), HamsaAIOutputFormatDefinition>
            {
                { (AudioEncodingTypeEnum.WAV, 16000, 16), new(AudioEncodingTypeEnum.WAV, 16000, 16) },
            };
            FormatToRequestParamMap = new ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), HamsaAIOutputFormatDefinition>(formatMap);
        }
    }
}