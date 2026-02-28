using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraCore.Entities.TTS.Providers.MurfAI;
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
    public class MurfAITTSService : ITTSService, IDisposable
    {
        private static readonly HttpClient _httpClient = new();
        private readonly ILogger<MurfAITTSService> _logger;
        private readonly string _apiKey;
        private readonly MurfAiConfig _serviceConfig;
        private readonly string _apiUrl;

        // State
        private AudioRequestDetails _finalUserRequest;
        private TTSProviderAvailableAudioFormat _optimalMurfFormat;
        private MurfOutputFormatDefinition _selectedApiFormat;
        private bool _audioConversationNeeded = false;
        private Dictionary<string, MurfPronunciationDetail>? _pronunciationDict;

        public MurfAITTSService(ILogger<MurfAITTSService> logger, string apiKey, MurfAiConfig config)
        {
            _logger = logger;
            _apiKey = apiKey;
            _serviceConfig = config;

            _apiUrl = $"https://{_serviceConfig.Region}.api.murf.ai/v1/speech/stream";

            // Parse Pronunciation Dictionary once
            if (!string.IsNullOrWhiteSpace(_serviceConfig.PronunciationDictionaryJson))
            {
                try
                {
                    _pronunciationDict = JsonSerializer.Deserialize<Dictionary<string, MurfPronunciationDetail>>(_serviceConfig.PronunciationDictionaryJson);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to parse Murf pronunciation dictionary JSON: {Msg}", ex.Message);
                }
            }
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

                var bestFallbackOrder = AudiEncoderFallbackSelector.GetFallbackOrder(_finalUserRequest, MurfSupportedFormats);
                _optimalMurfFormat = bestFallbackOrder.FirstOrDefault();

                if (_optimalMurfFormat == null)
                {
                    return result.SetFailureResult(
                        "Initialize:FORMAT_NOT_SUPPORTED",
                        $"Murf AI TTS does not support a format compatible with: {_finalUserRequest.RequestedEncoding} @ {_finalUserRequest.RequestedSampleRateHz}Hz"
                    );
                }

                var formatKey = (_optimalMurfFormat.Encoding, _optimalMurfFormat.SampleRateHz, _optimalMurfFormat.BitsPerSample);
                if (!FormatMap.TryGetValue(formatKey, out _selectedApiFormat))
                {
                    throw new InvalidOperationException($"Internal error: No mapping found for selected format: {formatKey}");
                }

                _audioConversationNeeded = _optimalMurfFormat.Encoding != _finalUserRequest.RequestedEncoding ||
                                           _optimalMurfFormat.SampleRateHz != _finalUserRequest.RequestedSampleRateHz ||
                                           _optimalMurfFormat.BitsPerSample != _finalUserRequest.RequestedBitsPerSample;

                var accountCheck = await CheckAccount();
                if (!accountCheck.Success)
                {
                    return result.SetFailureResult(accountCheck.Code, accountCheck.Message);
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("Initialize:EXCEPTION", $"Murf init error: {ex.Message}");
            }
        }

        public async Task<FunctionReturnResult> CheckAccount()
        {
            // Murf doesn't have a specific "Check Balance" endpoint documented publically that is lightweight.
            // We assume valid config. Auth errors will be caught in SynthesizeTextAsync.
            return await Task.FromResult(new FunctionReturnResult().SetSuccessResult());
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            if (string.IsNullOrEmpty(text)) return (Array.Empty<byte>(), TimeSpan.Zero);

            try
            {
                var requestPayload = new MurfStreamTtsRequest
                {
                    Text = text,
                    VoiceId = _serviceConfig.VoiceId,
                    Model = _serviceConfig.Model,
                    MultiNativeLocale = string.IsNullOrWhiteSpace(_serviceConfig.MultiNativeLocale) ? null : _serviceConfig.MultiNativeLocale,
                    Style = string.IsNullOrWhiteSpace(_serviceConfig.Style) ? null : _serviceConfig.Style,
                    Rate = _serviceConfig.Rate,
                    Pitch = _serviceConfig.Pitch,
                    Variation = _serviceConfig.Model == "GEN2" ? _serviceConfig.Variation : null,
                    SampleRate = _selectedApiFormat.SampleRate,
                    Format = "PCM", // Or WAV depending on API strictness for streaming
                    ChannelType = "MONO",
                    PronunciationDictionary = _pronunciationDict
                };

                string jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });

                using var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
                request.Headers.Add("api-key", _apiKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Murf HTTP Error {Code}: {Body}", response.StatusCode, errorBody);
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                // Streaming endpoint returns raw audio bytes directly
                byte[] sourceAudioData;
                using (var ms = new MemoryStream())
                {
                    await response.Content.CopyToAsync(ms, cancellationToken);
                    sourceAudioData = ms.ToArray();
                }

                if (sourceAudioData == null || sourceAudioData.Length == 0)
                {
                    _logger.LogError("Murf returned success but stream was empty.");
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                var duration = AudioConversationHelper.CalculateDuration(sourceAudioData, _optimalMurfFormat);

                if (_audioConversationNeeded)
                {
                    var (convertedData, _) = AudioConversationHelper.Convert(sourceAudioData, _optimalMurfFormat, _finalUserRequest, false);
                    return (convertedData, duration);
                }

                return (sourceAudioData, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Murf Synthesis Error");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        public Task StopTextSynthesisAsync() => Task.CompletedTask;

        public string GetProviderFullName() => "MurfAITextToSpeech";
        public InterfaceTTSProviderEnum GetProviderType() => GetProviderTypeStatic();
        public static InterfaceTTSProviderEnum GetProviderTypeStatic() => InterfaceTTSProviderEnum.MurfAITextToSpeech;
        public ITTSConfig GetCacheableConfig() => _serviceConfig;

        public TTSProviderAvailableAudioFormat GetCurrentOutputFormat() => _optimalMurfFormat;

        public void Dispose() => GC.SuppressFinalize(this);

        // =================================================================================================
        // STATIC DATA & MAPPINGS
        // =================================================================================================

        private record MurfOutputFormatDefinition(int SampleRate);

        private static readonly ReadOnlyCollection<TTSProviderAvailableAudioFormat> MurfSupportedFormats;
        private static readonly ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), MurfOutputFormatDefinition> FormatMap;

        static MurfAITTSService()
        {
            var supportedFormats = new List<TTSProviderAvailableAudioFormat>
            {
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 8000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 24000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 44100, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 48000, BitsPerSample = 16 },
            };
            MurfSupportedFormats = supportedFormats.AsReadOnly();

            var formatMap = new Dictionary<(AudioEncodingTypeEnum, int, int), MurfOutputFormatDefinition>
            {
                { (AudioEncodingTypeEnum.PCM, 8000, 16), new(8000) },
                { (AudioEncodingTypeEnum.PCM, 24000, 16), new(24000) },
                { (AudioEncodingTypeEnum.PCM, 44100, 16), new(44100) },
                { (AudioEncodingTypeEnum.PCM, 48000, 16), new(48000) },
            };
            FormatMap = new ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), MurfOutputFormatDefinition>(formatMap);
        }
    }
}