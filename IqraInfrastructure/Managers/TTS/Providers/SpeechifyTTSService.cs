using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraCore.Entities.TTS.Providers.Speechify;
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
    public class SpeechifyTTSService : ITTSService, IDisposable
    {
        private readonly ILogger<SpeechifyTTSService> _logger;
        private readonly string _apiKey;
        private readonly SpeechifyConfig _serviceConfig;

        private const string ApiUrl = "https://api.sws.speechify.com/v1/audio/speech";
        private static readonly HttpClient _httpClient = new();

        // State
        private AudioRequestDetails _finalUserRequest;
        private TTSProviderAvailableAudioFormat _optimalSpeechifyFormat;
        private SpeechifyOutputFormatDefinition _selectedApiFormat;
        private bool _audioConversationNeeded = false;

        public SpeechifyTTSService(ILogger<SpeechifyTTSService> logger, string apiKey, SpeechifyConfig config)
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

                var bestFallbackOrder = AudiEncoderFallbackSelector.GetFallbackOrder(_finalUserRequest, SpeechifySupportedFormats);
                _optimalSpeechifyFormat = bestFallbackOrder.FirstOrDefault();

                if (_optimalSpeechifyFormat == null)
                {
                    return result.SetFailureResult(
                        "Initialize:FORMAT_NOT_SUPPORTED",
                        $"Speechify TTS does not support a format compatible with: {_finalUserRequest.RequestedEncoding}"
                    );
                }

                var formatKey = (_optimalSpeechifyFormat.Encoding, _optimalSpeechifyFormat.SampleRateHz, _optimalSpeechifyFormat.BitsPerSample);
                if (!FormatMap.TryGetValue(formatKey, out _selectedApiFormat))
                {
                    throw new InvalidOperationException($"Internal error: No mapping found for selected format: {formatKey}");
                }

                _audioConversationNeeded = _optimalSpeechifyFormat.Encoding != _finalUserRequest.RequestedEncoding ||
                                           _optimalSpeechifyFormat.SampleRateHz != _finalUserRequest.RequestedSampleRateHz ||
                                           _optimalSpeechifyFormat.BitsPerSample != _finalUserRequest.RequestedBitsPerSample;

                var accountCheck = await CheckAccount();
                if (!accountCheck.Success)
                {
                    return result.SetFailureResult(accountCheck.Code, accountCheck.Message);
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("Initialize:EXCEPTION", $"Speechify init error: {ex.Message}");
            }
        }

        public async Task<FunctionReturnResult> CheckAccount()
        {
            // Speechify SWS API does not expose a dedicated balance/auth check endpoint.
            // We assume success and let synthesis fail if auth is bad.
            return await Task.FromResult(new FunctionReturnResult().SetSuccessResult());
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            if (string.IsNullOrEmpty(text)) return (Array.Empty<byte>(), TimeSpan.Zero);

            try
            {
                var requestPayload = new SpeechifyTtsRequest
                {
                    Input = text,
                    VoiceId = _serviceConfig.VoiceId,
                    Model = _serviceConfig.Model,
                    AudioFormat = _selectedApiFormat.FormatString,
                    Language = _serviceConfig.Language,
                    Options = new SpeechifyOptionsRequest
                    {
                        LoudnessNormalization = _serviceConfig.LoudnessNormalization,
                        TextNormalization = _serviceConfig.TextNormalization
                    }
                };

                string jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });

                using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    try
                    {
                        var errorResp = JsonSerializer.Deserialize<SpeechifyTtsResponse>(responseBody);
                        if (!string.IsNullOrEmpty(errorResp?.ErrorMessage))
                            _logger.LogError("Speechify API Error: {Msg}", errorResp.ErrorMessage);
                    }
                    catch { _logger.LogError("Speechify HTTP Error {Code}: {Body}", response.StatusCode, responseBody); }

                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                var ttsResponse = JsonSerializer.Deserialize<SpeechifyTtsResponse>(responseBody);

                if (string.IsNullOrEmpty(ttsResponse?.AudioData))
                {
                    _logger.LogError("Speechify returned success but no audio data.");
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                byte[] sourceAudioData = Convert.FromBase64String(ttsResponse.AudioData);

                var duration = AudioConversationHelper.CalculateDuration(sourceAudioData, _optimalSpeechifyFormat);

                if (_audioConversationNeeded)
                {
                    var (convertedData, _) = AudioConversationHelper.Convert(sourceAudioData, _optimalSpeechifyFormat, _finalUserRequest, false);
                    return (convertedData, duration);
                }

                return (sourceAudioData, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Speechify Synthesis Error");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        public Task StopTextSynthesisAsync() => Task.CompletedTask;

        public string GetProviderFullName() => "SpeechifyTextToSpeech";
        public InterfaceTTSProviderEnum GetProviderType() => GetProviderTypeStatic();
        public static InterfaceTTSProviderEnum GetProviderTypeStatic() => InterfaceTTSProviderEnum.SpeechifyTextToSpeech;
        public ITTSConfig GetCacheableConfig() => _serviceConfig;

        public TTSProviderAvailableAudioFormat GetCurrentOutputFormat() => _optimalSpeechifyFormat;

        public void Dispose() => GC.SuppressFinalize(this);

        // =================================================================================================
        // STATIC DATA & MAPPINGS
        // =================================================================================================

        private record SpeechifyOutputFormatDefinition(string FormatString);

        private static readonly ReadOnlyCollection<TTSProviderAvailableAudioFormat> SpeechifySupportedFormats;
        private static readonly ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), SpeechifyOutputFormatDefinition> FormatMap;

        static SpeechifyTTSService()
        {
            // Speechify's "pcm" format is fixed at 24kHz 16-bit Mono.
            var supportedFormats = new List<TTSProviderAvailableAudioFormat>
            {
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 24000, BitsPerSample = 16 },
            };
            SpeechifySupportedFormats = supportedFormats.AsReadOnly();

            var formatMap = new Dictionary<(AudioEncodingTypeEnum, int, int), SpeechifyOutputFormatDefinition>
            {
                { (AudioEncodingTypeEnum.PCM, 24000, 16), new("pcm") },
            };
            FormatMap = new ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), SpeechifyOutputFormatDefinition>(formatMap);
        }
    }
}