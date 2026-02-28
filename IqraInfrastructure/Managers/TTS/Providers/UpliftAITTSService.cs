using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraCore.Entities.TTS.Providers.UpliftAI;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.TTS;
using IqraInfrastructure.Helpers.Audio;
using IqraInfrastructure.Managers.TTS.Helpers;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class UpliftAITTSService : ITTSService, IDisposable
    {
        private static readonly HttpClient _httpClient = new();
        private readonly ILogger<UpliftAITTSService> _logger;
        private readonly string _apiKey;
        private readonly UpliftAiConfig _serviceConfig;

        private const string ApiUrl = "https://api.upliftai.org/v1/synthesis/text-to-speech/stream";

        // State
        private AudioRequestDetails _finalUserRequest;
        private TTSProviderAvailableAudioFormat _optimalUpliftFormat;
        private UpliftOutputFormatDefinition _selectedApiFormat;
        private bool _audioConversationNeeded = false;

        public UpliftAITTSService(ILogger<UpliftAITTSService> logger, string apiKey, UpliftAiConfig config)
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

                var bestFallbackOrder = AudiEncoderFallbackSelector.GetFallbackOrder(_finalUserRequest, UpliftSupportedFormats);
                _optimalUpliftFormat = bestFallbackOrder.FirstOrDefault();

                if (_optimalUpliftFormat == null)
                {
                    return result.SetFailureResult(
                        "Initialize:FORMAT_NOT_SUPPORTED",
                        $"Uplift AI TTS does not support a format compatible with: {_finalUserRequest.RequestedEncoding} @ {_finalUserRequest.RequestedSampleRateHz}Hz"
                    );
                }

                var formatKey = (_optimalUpliftFormat.Encoding, _optimalUpliftFormat.SampleRateHz, _optimalUpliftFormat.BitsPerSample);
                if (!FormatMap.TryGetValue(formatKey, out _selectedApiFormat))
                {
                    throw new InvalidOperationException($"Internal error: No mapping found for selected format: {formatKey}");
                }

                _audioConversationNeeded = _optimalUpliftFormat.Encoding != _finalUserRequest.RequestedEncoding ||
                                           _optimalUpliftFormat.SampleRateHz != _finalUserRequest.RequestedSampleRateHz ||
                                           _optimalUpliftFormat.BitsPerSample != _finalUserRequest.RequestedBitsPerSample;

                var accountCheck = await CheckAccount();
                if (!accountCheck.Success)
                {
                    return result.SetFailureResult(accountCheck.Code, accountCheck.Message);
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("Initialize:EXCEPTION", $"Uplift AI init error: {ex.Message}");
            }
        }

        public async Task<FunctionReturnResult> CheckAccount()
        {
            // No dedicated balance endpoint. Assume success.
            return await Task.FromResult(new FunctionReturnResult().SetSuccessResult());
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            if (string.IsNullOrEmpty(text)) return (Array.Empty<byte>(), TimeSpan.Zero);

            try
            {
                var requestPayload = new UpliftTtsRequest
                {
                    VoiceId = _serviceConfig.VoiceId,
                    Text = text,
                    OutputFormat = _selectedApiFormat.FormatString,
                    PhraseReplacementConfigId = _serviceConfig.PhraseReplacementConfigId
                };

                string jsonPayload = JsonSerializer.Serialize(requestPayload);

                using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Uplift AI API Error {Code}: {Body}", response.StatusCode, errorContent);
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
                    _logger.LogWarning("Uplift AI returned 0 bytes.");
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                var duration = AudioConversationHelper.CalculateDuration(sourceAudioData, _optimalUpliftFormat);

                if (_audioConversationNeeded)
                {
                    var (convertedData, _) = AudioConversationHelper.Convert(sourceAudioData, _optimalUpliftFormat, _finalUserRequest, false);
                    return (convertedData, duration);
                }

                return (sourceAudioData, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Uplift AI Synthesis Error");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        public Task StopTextSynthesisAsync() => Task.CompletedTask;

        public string GetProviderFullName() => "UpliftAITextToSpeech";
        public InterfaceTTSProviderEnum GetProviderType() => GetProviderTypeStatic();
        public static InterfaceTTSProviderEnum GetProviderTypeStatic() => InterfaceTTSProviderEnum.UpliftAITextToSpeech;
        public ITTSConfig GetCacheableConfig() => _serviceConfig;

        public TTSProviderAvailableAudioFormat GetCurrentOutputFormat() => _optimalUpliftFormat;

        public void Dispose() => GC.SuppressFinalize(this);

        // =================================================================================================
        // STATIC DATA & MAPPINGS
        // =================================================================================================

        private record UpliftOutputFormatDefinition(string FormatString);

        private static readonly ReadOnlyCollection<TTSProviderAvailableAudioFormat> UpliftSupportedFormats;
        private static readonly ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), UpliftOutputFormatDefinition> FormatMap;

        static UpliftAITTSService()
        {
            var supportedFormats = new List<TTSProviderAvailableAudioFormat>
            {
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 22050, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 22050, BitsPerSample = 32 },
            };
            UpliftSupportedFormats = supportedFormats.AsReadOnly();

            var formatMap = new Dictionary<(AudioEncodingTypeEnum, int, int), UpliftOutputFormatDefinition>
            {
                { (AudioEncodingTypeEnum.WAV, 22050, 16), new("WAV_22050_16") },
                { (AudioEncodingTypeEnum.WAV, 22050, 32), new("WAV_22050_32") },
            };
            FormatMap = new ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), UpliftOutputFormatDefinition>(formatMap);
        }
    }
}