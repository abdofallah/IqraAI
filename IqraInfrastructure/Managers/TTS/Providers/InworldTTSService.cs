using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraCore.Entities.TTS.Providers.Inworld;
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
    public class InworldTTSService : ITTSService, IDisposable
    {
        private static readonly HttpClient _httpClient = new();
        private readonly ILogger<InworldTTSService> _logger;
        private readonly string _apiKey;
        private readonly InworldConfig _serviceConfig;

        private const string ApiUrl = "https://api.inworld.ai/tts/v1/voice";

        // State
        private AudioRequestDetails _finalUserRequest;
        private TTSProviderAvailableAudioFormat _optimalInworldFormat;
        private InworldOutputFormatDefinition _selectedApiFormat;
        private bool _audioConversationNeeded = false;

        // JSON Options
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public InworldTTSService(ILogger<InworldTTSService> logger, string apiKey, InworldConfig config)
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

                var bestFallbackOrder = AudiEncoderFallbackSelector.GetFallbackOrder(_finalUserRequest, InworldSupportedFormats);
                _optimalInworldFormat = bestFallbackOrder.FirstOrDefault();

                if (_optimalInworldFormat == null)
                {
                    return result.SetFailureResult(
                        "Initialize:FORMAT_NOT_SUPPORTED",
                        $"Inworld TTS does not support a format compatible with: {_finalUserRequest.RequestedEncoding} @ {_finalUserRequest.RequestedSampleRateHz}Hz"
                    );
                }

                var formatKey = (_optimalInworldFormat.Encoding, _optimalInworldFormat.SampleRateHz, _optimalInworldFormat.BitsPerSample);
                if (!FormatMap.TryGetValue(formatKey, out _selectedApiFormat))
                {
                    throw new InvalidOperationException($"Internal error: No mapping found for selected format: {formatKey}");
                }

                _audioConversationNeeded = _optimalInworldFormat.Encoding != _finalUserRequest.RequestedEncoding ||
                                           _optimalInworldFormat.SampleRateHz != _finalUserRequest.RequestedSampleRateHz ||
                                           _optimalInworldFormat.BitsPerSample != _finalUserRequest.RequestedBitsPerSample;

                var accountCheck = await CheckAccount();
                if (!accountCheck.Success)
                {
                    return result.SetFailureResult(accountCheck.Code, accountCheck.Message);
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("Initialize:EXCEPTION", $"Inworld init error: {ex.Message}");
            }
        }

        public async Task<FunctionReturnResult> CheckAccount()
        {
            // No auth endpoint provided in docs. Assume success.
            return await Task.FromResult(new FunctionReturnResult().SetSuccessResult());
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            if (string.IsNullOrEmpty(text)) return (Array.Empty<byte>(), TimeSpan.Zero);

            try
            {
                var requestPayload = new InworldTtsRequest
                {
                    Text = text,
                    VoiceId = _serviceConfig.VoiceName,
                    ModelId = _serviceConfig.Model,
                    Temperature = _serviceConfig.Temperature,
                    AudioConfig = new InworldAudioConfig
                    {
                        AudioEncoding = _selectedApiFormat.EncodingString,
                        SampleRateHertz = _selectedApiFormat.SampleRate,
                        SpeakingRate = _serviceConfig.Speed,
                    }
                };

                string jsonPayload = JsonSerializer.Serialize(requestPayload, _jsonOptions);

                using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _apiKey);
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                InworldTtsResponse? apiResponse = null;
                try
                {
                    apiResponse = JsonSerializer.Deserialize<InworldTtsResponse>(responseBody, _jsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize Inworld response: {Body}", responseBody);
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Inworld API HTTP {Code}. Error: {Message}",
                        response.StatusCode,
                        apiResponse?.Error?.Message ?? responseBody);
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                if (apiResponse?.Error != null)
                {
                    _logger.LogError("Inworld API Logic Error {Code}: {Message}",
                        apiResponse.Error.Code,
                        apiResponse.Error.Message);
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                if (string.IsNullOrEmpty(apiResponse?.Result?.AudioContent))
                {
                    _logger.LogWarning("Inworld returned success but no audio content.");
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                byte[] sourceAudioData = Convert.FromBase64String(apiResponse.Result.AudioContent);

                var duration = AudioConversationHelper.CalculateDuration(sourceAudioData, _optimalInworldFormat);

                if (_audioConversationNeeded)
                {
                    var (convertedData, _) = AudioConversationHelper.Convert(sourceAudioData, _optimalInworldFormat, _finalUserRequest, false);
                    return (convertedData, duration);
                }

                return (sourceAudioData, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inworld Synthesis Error");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        public Task StopTextSynthesisAsync() => Task.CompletedTask;

        public string GetProviderFullName() => "InworldTextToSpeech";
        public InterfaceTTSProviderEnum GetProviderType() => GetProviderTypeStatic();
        public static InterfaceTTSProviderEnum GetProviderTypeStatic() => InterfaceTTSProviderEnum.InworldTextToSpeech;
        public ITTSConfig GetCacheableConfig() => _serviceConfig;

        public TTSProviderAvailableAudioFormat GetCurrentOutputFormat() => _optimalInworldFormat;

        public void Dispose() => GC.SuppressFinalize(this);

        // =================================================================================================
        // STATIC DATA & MAPPINGS
        // =================================================================================================

        private record InworldOutputFormatDefinition(string EncodingString, int SampleRate);

        private static readonly ReadOnlyCollection<TTSProviderAvailableAudioFormat> InworldSupportedFormats;
        private static readonly ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), InworldOutputFormatDefinition> FormatMap;

        static InworldTTSService()
        {
            var supportedFormats = new List<TTSProviderAvailableAudioFormat>
            {
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 8000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 16000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 22050, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 24000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 32000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 44100, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 48000, BitsPerSample = 16 },
            };
            InworldSupportedFormats = supportedFormats.AsReadOnly();

            var formatMap = new Dictionary<(AudioEncodingTypeEnum, int, int), InworldOutputFormatDefinition>();
            foreach (var fmt in supportedFormats)
            {
                formatMap.Add((fmt.Encoding, fmt.SampleRateHz, fmt.BitsPerSample), new InworldOutputFormatDefinition("LINEAR16", fmt.SampleRateHz));
            }
            FormatMap = new ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), InworldOutputFormatDefinition>(formatMap);
        }
    }
}