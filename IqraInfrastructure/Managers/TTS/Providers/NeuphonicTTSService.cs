using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraCore.Entities.TTS.Providers.Neuphonic;
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
    public class NeuphonicTTSService : ITTSService, IDisposable
    {
        private readonly ILogger<NeuphonicTTSService> _logger;
        private readonly string _apiKey;
        private readonly NeuphonicConfig _serviceConfig;

        private static readonly HttpClient _httpClient = new();
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        private const string BaseApiUrl = "https://api.neuphonic.com";

        // State
        private AudioRequestDetails _finalUserRequest;
        private TTSProviderAvailableAudioFormat _optimalNeuphonicFormat;
        private NeuphonicOutputFormatDefinition _selectedApiFormat;
        private bool _audioConversationNeeded = false;

        public NeuphonicTTSService(ILogger<NeuphonicTTSService> logger, string apiKey, NeuphonicConfig config)
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

                var bestFallbackOrder = AudiEncoderFallbackSelector.GetFallbackOrder(_finalUserRequest, NeuphonicSupportedFormats);
                _optimalNeuphonicFormat = bestFallbackOrder.FirstOrDefault();

                if (_optimalNeuphonicFormat == null)
                {
                    return result.SetFailureResult(
                        "Initialize:FORMAT_NOT_SUPPORTED",
                        $"Neuphonic TTS does not support a format compatible with: {_finalUserRequest.RequestedEncoding} @ {_finalUserRequest.RequestedSampleRateHz}Hz"
                    );
                }

                var formatKey = (_optimalNeuphonicFormat.Encoding, _optimalNeuphonicFormat.SampleRateHz, _optimalNeuphonicFormat.BitsPerSample);
                if (!FormatMap.TryGetValue(formatKey, out _selectedApiFormat))
                {
                    throw new InvalidOperationException($"Internal error: No mapping found for selected format: {formatKey}");
                }

                _audioConversationNeeded = _optimalNeuphonicFormat.Encoding != _finalUserRequest.RequestedEncoding ||
                                           _optimalNeuphonicFormat.SampleRateHz != _finalUserRequest.RequestedSampleRateHz ||
                                           _optimalNeuphonicFormat.BitsPerSample != _finalUserRequest.RequestedBitsPerSample;

                var accountCheck = await CheckAccount();
                if (!accountCheck.Success)
                {
                    return result.SetFailureResult(accountCheck.Code, accountCheck.Message);
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("Initialize:EXCEPTION", $"Neuphonic init error: {ex.Message}");
            }
        }

        public async Task<FunctionReturnResult> CheckAccount()
        {
            // No explicit balance check API. Assume success.
            return await Task.FromResult(new FunctionReturnResult().SetSuccessResult());
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            if (string.IsNullOrEmpty(text)) return (Array.Empty<byte>(), TimeSpan.Zero);

            var requestUrl = $"{BaseApiUrl}/sse/speak/{_serviceConfig.LanguageCode}";

            var apiRequestPayload = new NeuphonicTtsApiRequest
            {
                Text = text,
                VoiceId = _serviceConfig.VoiceId,
                SamplingRate = _selectedApiFormat.SampleRate,
                Speed = _serviceConfig.Speed,
                Temperature = _serviceConfig.Temperature,
                Encoding = "pcm_linear"
            };

            string jsonPayload = JsonSerializer.Serialize(apiRequestPayload, _jsonSerializerOptions);

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Add("X-API-KEY", _apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Neuphonic API Error {Code}: {Body}", response.StatusCode, errorBody);
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                using var reader = new StreamReader(stream, Encoding.UTF8);

                var accumulatedAudioBytes = new List<byte>();

                string? line;
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    line = line.Trim();

                    if (string.IsNullOrEmpty(line)) continue;

                    // Skip event type declarations if they send them (e.g. "event: message")
                    if (line.StartsWith("event:")) continue;

                    if (line.StartsWith("data:"))
                    {
                        var json = line.Substring(5).Trim();

                        if (json == "[DONE]") break;
                        if (string.IsNullOrEmpty(json)) continue;

                        try
                        {
                            var sseEventPayload = JsonSerializer.Deserialize<NeuphonicSseEventPayload>(json, _jsonSerializerOptions);

                            if (sseEventPayload?.Errors != null && sseEventPayload.Errors.Any())
                            {
                                _logger.LogError("Neuphonic SSE Error: {Errors}", string.Join(", ", sseEventPayload.Errors));
                                break; // Stop processing on error
                            }

                            if (sseEventPayload?.StatusCode == 200 && !string.IsNullOrEmpty(sseEventPayload?.Data?.Audio))
                            {
                                byte[] audioChunk = Convert.FromBase64String(sseEventPayload.Data.Audio);
                                accumulatedAudioBytes.AddRange(audioChunk);
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning("Neuphonic SSE Parse Error: {Message}. Raw JSON: {Json}", ex.Message, json);
                        }
                    }
                }

                if (accumulatedAudioBytes.Count == 0)
                {
                    _logger.LogWarning("Neuphonic returned no audio bytes.");
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                byte[] sourceAudioData = accumulatedAudioBytes.ToArray();

                var duration = AudioConversationHelper.CalculateDuration(sourceAudioData, _optimalNeuphonicFormat);

                if (_audioConversationNeeded)
                {
                    var (convertedData, _) = AudioConversationHelper.Convert(sourceAudioData, _optimalNeuphonicFormat, _finalUserRequest, false);
                    return (convertedData, duration);
                }

                return (sourceAudioData, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Neuphonic Synthesis Error");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        public Task StopTextSynthesisAsync() => Task.CompletedTask;

        public string GetProviderFullName() => "NeuphonicTextToSpeech";
        public InterfaceTTSProviderEnum GetProviderType() => GetProviderTypeStatic();
        public static InterfaceTTSProviderEnum GetProviderTypeStatic() => InterfaceTTSProviderEnum.NeuphonicTextToSpeech;
        public ITTSConfig GetCacheableConfig() => _serviceConfig;

        public TTSProviderAvailableAudioFormat GetCurrentOutputFormat() => _optimalNeuphonicFormat;

        public void Dispose() => GC.SuppressFinalize(this);

        // =================================================================================================
        // STATIC DATA & MAPPINGS
        // =================================================================================================

        private record NeuphonicOutputFormatDefinition(int SampleRate);

        private static readonly ReadOnlyCollection<TTSProviderAvailableAudioFormat> NeuphonicSupportedFormats;
        private static readonly ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), NeuphonicOutputFormatDefinition> FormatMap;

        static NeuphonicTTSService()
        {
            var supportedFormats = new List<TTSProviderAvailableAudioFormat>
            {
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 8000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 16000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 22050, BitsPerSample = 16 },
            };
            NeuphonicSupportedFormats = supportedFormats.AsReadOnly();

            var formatMap = new Dictionary<(AudioEncodingTypeEnum, int, int), NeuphonicOutputFormatDefinition>
            {
                { (AudioEncodingTypeEnum.PCM, 8000, 16), new(8000) },
                { (AudioEncodingTypeEnum.PCM, 16000, 16), new(16000) },
                { (AudioEncodingTypeEnum.PCM, 22050, 16), new(22050) },
            };
            FormatMap = new ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), NeuphonicOutputFormatDefinition>(formatMap);
        }
    }
}