using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraCore.Entities.TTS.Providers.Hamsa;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.TTS;
using IqraInfrastructure.Helpers.Audio;
using IqraInfrastructure.Managers.TTS.Helpers;
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

        private readonly string _apiKey;
        private const string ApiUrl = "https://api.tryhamsa.com/v1/realtime/tts";
        private readonly HamsaAiConfig _serviceConfig;

        private AudioRequestDetails _finalUserRequest;
        private TTSProviderAvailableAudioFormat _optimalHamsaFormat;
        private bool _audioConversationNeeded = false;

        private bool _requestMuLaw = false;

        public HamsaAITTSService(string apiKey, HamsaAiConfig config)
        {
            _apiKey = apiKey;
            _serviceConfig = config;
        }

        public void Initialize()
        {
            // 1. Define what the user ultimately wants.
            _finalUserRequest = new AudioRequestDetails
            {
                RequestedEncoding = _serviceConfig.TargetEncodingType,
                RequestedSampleRateHz = _serviceConfig.TargetSampleRate,
                RequestedBitsPerSample = _serviceConfig.TargetBitsPerSample
            };

            // 2. Use the selector to find the best format Hamsa can provide.
            var bestFallbackOrder = AudiEncoderFallbackSelector.GetFallbackOrder(_finalUserRequest, HamsaSupportedFormats);
            _optimalHamsaFormat = bestFallbackOrder.FirstOrDefault() ?? throw new NotSupportedException(
                "Hamsa AI TTS does not support any format that can be reasonably converted to the requested format.");

            // 3. Find the corresponding API parameter for the chosen optimal format.
            var formatKey = (_optimalHamsaFormat.Encoding, _optimalHamsaFormat.SampleRateHz, _optimalHamsaFormat.BitsPerSample);
            if (!FormatToRequestParamMap.TryGetValue(formatKey, out _requestMuLaw)) // Set the class-level field
            {
                throw new InvalidOperationException($"Internal error: No mapping found for the selected optimal Hamsa format: {formatKey}");
            }

            // 4. Determine if a final conversion step will be needed after synthesis.
            _audioConversationNeeded = _optimalHamsaFormat.Encoding != _finalUserRequest.RequestedEncoding ||
                                    _optimalHamsaFormat.SampleRateHz != _finalUserRequest.RequestedSampleRateHz ||
                                    _optimalHamsaFormat.BitsPerSample != _finalUserRequest.RequestedBitsPerSample;
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            var requestPayload = new HamsaTtsApiRequest
            {
                Text = text,
                Speaker = _serviceConfig.Speaker,
                Dialect = _serviceConfig.Dialect,
                MuLaw = _requestMuLaw
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
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                byte[] sourceAudioData = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                (byte[], TimeSpan) finalAudioData = (sourceAudioData, AudioConversationHelper.CalculateDuration(sourceAudioData, _optimalHamsaFormat));

                if (_audioConversationNeeded)
                {
                    finalAudioData = AudioConversationHelper.Convert(sourceAudioData, _optimalHamsaFormat, _finalUserRequest);
                }

                return finalAudioData;
            }
            catch (HttpRequestException)
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (JsonException)
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (Exception)
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        public Task StopTextSynthesisAsync()
        {
            return Task.CompletedTask;
        }

        public string GetProviderFullName()
        {
            return "HamsaAITextToSpeech";
        }

        public InterfaceTTSProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public static InterfaceTTSProviderEnum GetProviderTypeStatic()
        {
            return InterfaceTTSProviderEnum.HamsaAITextToSpeech;
        }

        public ITTSConfig GetCacheableConfig()
        {
            return _serviceConfig;
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        // STATIC
        private static readonly ReadOnlyCollection<TTSProviderAvailableAudioFormat> HamsaSupportedFormats;
        private static readonly ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), bool> FormatToRequestParamMap;

        static HamsaAITTSService()
        {
            // Hamsa has a very simple capability set: one PCM format and one MULAW format.
            var supportedFormats = new List<TTSProviderAvailableAudioFormat>
            {
                // This is what Hamsa provides when `MuLaw = false`
                new() { Encoding = AudioEncodingTypeEnum.WAV, SampleRateHz = 16000, BitsPerSample = 16 },

                // This is what Hamsa provides when `MuLaw = true`
                new() { Encoding = AudioEncodingTypeEnum.MULAW, SampleRateHz = 8000, BitsPerSample = 8 },
            };
            HamsaSupportedFormats = supportedFormats.AsReadOnly();

            // Create a simple mapping from our format definition to the boolean API parameter.
            var formatMap = new Dictionary<(AudioEncodingTypeEnum, int, int), bool>
            {
                // Key: Our format definition, Value: The value for the `MuLaw` parameter
                { (AudioEncodingTypeEnum.WAV, 16000, 16), false },
                { (AudioEncodingTypeEnum.MULAW, 8000, 8), true },
            };
            FormatToRequestParamMap = new ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), bool>(formatMap);
        }
    }
}
