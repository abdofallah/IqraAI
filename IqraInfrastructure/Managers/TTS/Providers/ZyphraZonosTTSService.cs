using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraCore.Entities.TTS.Providers.ZyphraZonos;
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
    public class ZyphraZonosTTSService : ITTSService, IDisposable
    {
        private static readonly HttpClient _httpClient = new();
        private readonly ILogger<ZyphraZonosTTSService> _logger;
        private readonly string _apiKey;
        private readonly ZyphraZonosConfig _serviceConfig;

        private const string ApiUrl = "http://api.zyphra.com/v1/audio/text-to-speech";

        // State
        private AudioRequestDetails _finalUserRequest;
        private TTSProviderAvailableAudioFormat _optimalZyphraFormat;
        private ZyphraOutputFormatDefinition _selectedApiFormat;
        private bool _audioConversationNeeded = false;

        public ZyphraZonosTTSService(ILogger<ZyphraZonosTTSService> logger, string apiKey, ZyphraZonosConfig config)
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

                var bestFallbackOrder = AudiEncoderFallbackSelector.GetFallbackOrder(_finalUserRequest, ZyphraSupportedFormats);
                _optimalZyphraFormat = bestFallbackOrder.FirstOrDefault();

                if (_optimalZyphraFormat == null)
                {
                    return result.SetFailureResult(
                        "Initialize:FORMAT_NOT_SUPPORTED",
                        $"Zyphra TTS does not support a format compatible with: {_finalUserRequest.RequestedEncoding}"
                    );
                }

                var formatKey = (_optimalZyphraFormat.Encoding, _optimalZyphraFormat.SampleRateHz, _optimalZyphraFormat.BitsPerSample);
                if (!FormatMap.TryGetValue(formatKey, out _selectedApiFormat))
                {
                    throw new InvalidOperationException($"Internal error: No mapping found for selected format: {formatKey}");
                }

                _audioConversationNeeded = _optimalZyphraFormat.Encoding != _finalUserRequest.RequestedEncoding ||
                                           _optimalZyphraFormat.SampleRateHz != _finalUserRequest.RequestedSampleRateHz ||
                                           _optimalZyphraFormat.BitsPerSample != _finalUserRequest.RequestedBitsPerSample;

                var accountCheck = await CheckAccount();
                if (!accountCheck.Success)
                {
                    return result.SetFailureResult(accountCheck.Code, accountCheck.Message);
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("Initialize:EXCEPTION", $"Zyphra init error: {ex.Message}");
            }
        }

        public async Task<FunctionReturnResult> CheckAccount()
        {
            // No public auth check endpoint available in docs. Assume success.
            return await Task.FromResult(new FunctionReturnResult().SetSuccessResult());
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            if (string.IsNullOrEmpty(text)) return (Array.Empty<byte>(), TimeSpan.Zero);

            try
            {
                var requestPayload = new ZyphraTtsRequest
                {
                    Text = text,
                    Model = _serviceConfig.Model,
                    MimeType = _selectedApiFormat.MimeType, // e.g. "audio/wav"
                    SpeakingRate = _serviceConfig.SpeakingRate,
                    LanguageIsoCode = !string.IsNullOrEmpty(_serviceConfig.LanguageIsoCode) ? _serviceConfig.LanguageIsoCode : null,
                    DefaultVoiceName = !string.IsNullOrEmpty(_serviceConfig.DefaultVoiceName) ? _serviceConfig.DefaultVoiceName : null,
                    VoiceName = _serviceConfig.DefaultVoiceName,
                    Emotion = _serviceConfig.Emotion,
                    Vqscore = _serviceConfig.Vqscore,
                    SpeakerNoised = _serviceConfig.SpeakerNoised
                };

                string jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });

                using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
                request.Headers.Add("X-API-Key", _apiKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(_selectedApiFormat.MimeType));
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Zyphra API Error {Code}: {Body}", response.StatusCode, errorContent);
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                byte[] sourceAudioData = await response.Content.ReadAsByteArrayAsync(cancellationToken);

                var duration = AudioConversationHelper.CalculateDuration(sourceAudioData, _optimalZyphraFormat);

                if (_audioConversationNeeded)
                {
                    var (convertedData, _) = AudioConversationHelper.Convert(sourceAudioData, _optimalZyphraFormat, _finalUserRequest, false);
                    return (convertedData, duration);
                }

                return (sourceAudioData, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Zyphra Synthesis Error");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        public Task StopTextSynthesisAsync() => Task.CompletedTask;

        public string GetProviderFullName() => "ZyphraZonosTextToSpeech";
        public InterfaceTTSProviderEnum GetProviderType() => GetProviderTypeStatic();
        public static InterfaceTTSProviderEnum GetProviderTypeStatic() => InterfaceTTSProviderEnum.ZyphraZonosTextToSpeech;
        public ITTSConfig GetCacheableConfig() => _serviceConfig;

        public TTSProviderAvailableAudioFormat GetCurrentOutputFormat() => _optimalZyphraFormat;

        public void Dispose() => GC.SuppressFinalize(this);

        // =================================================================================================
        // STATIC DATA & MAPPINGS
        // =================================================================================================

        private record ZyphraOutputFormatDefinition(string MimeType, int SampleRate);

        private static readonly ReadOnlyCollection<TTSProviderAvailableAudioFormat> ZyphraSupportedFormats;
        private static readonly ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), ZyphraOutputFormatDefinition> FormatMap;

        static ZyphraZonosTTSService()
        {
            var supportedFormats = new List<TTSProviderAvailableAudioFormat>
            {
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 48000, BitsPerSample = 16 },
            };
            ZyphraSupportedFormats = supportedFormats.AsReadOnly();

            var formatMap = new Dictionary<(AudioEncodingTypeEnum, int, int), ZyphraOutputFormatDefinition>
            {
                { (AudioEncodingTypeEnum.WAV, 48000, 16), new("audio/wav", 48000) }
            };
            FormatMap = new ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), ZyphraOutputFormatDefinition>(formatMap);
        }
    }
}