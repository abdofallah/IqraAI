using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraCore.Entities.TTS.Providers.Rime;
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
    public class RimeTTSService : ITTSService, IDisposable
    {
        private static readonly HttpClient _httpClient = new();
        private readonly ILogger<RimeTTSService> _logger;
        private readonly string _apiKey;
        private readonly RimeConfig _serviceConfig;

        private const string ApiUrl = "https://users.rime.ai/v1/rime-tts";

        // State
        private AudioRequestDetails _finalUserRequest;
        private TTSProviderAvailableAudioFormat _optimalRimeFormat;
        private RimeOutputFormatDefinition _selectedApiFormat;
        private bool _audioConversationNeeded = false;

        public RimeTTSService(ILogger<RimeTTSService> logger, string apiKey, RimeConfig config)
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

                var bestFallbackOrder = AudiEncoderFallbackSelector.GetFallbackOrder(_finalUserRequest, RimeSupportedFormats);
                _optimalRimeFormat = bestFallbackOrder.FirstOrDefault();

                if (_optimalRimeFormat == null)
                {
                    return result.SetFailureResult(
                        "Initialize:FORMAT_NOT_SUPPORTED",
                        $"Rime TTS does not support a format compatible with: {_finalUserRequest.RequestedEncoding} @ {_finalUserRequest.RequestedSampleRateHz}Hz"
                    );
                }

                var formatKey = (_optimalRimeFormat.Encoding, _optimalRimeFormat.SampleRateHz, _optimalRimeFormat.BitsPerSample);
                if (!FormatMap.TryGetValue(formatKey, out _selectedApiFormat))
                {
                    throw new InvalidOperationException($"Internal error: No mapping found for selected format: {formatKey}");
                }

                _audioConversationNeeded = _optimalRimeFormat.Encoding != _finalUserRequest.RequestedEncoding ||
                                           _optimalRimeFormat.SampleRateHz != _finalUserRequest.RequestedSampleRateHz ||
                                           _optimalRimeFormat.BitsPerSample != _finalUserRequest.RequestedBitsPerSample;

                var accountCheck = await CheckAccount();
                if (!accountCheck.Success)
                {
                    return result.SetFailureResult(accountCheck.Code, accountCheck.Message);
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("Initialize:EXCEPTION", $"Rime init error: {ex.Message}");
            }
        }

        public async Task<FunctionReturnResult> CheckAccount()
        {
            // Rime doesn't have a lightweight "Check Balance" endpoint accessible by API Key easily.
            // We assume valid config. Auth errors will be caught in SynthesizeTextAsync.
            return await Task.FromResult(new FunctionReturnResult().SetSuccessResult());
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            if (string.IsNullOrEmpty(text)) return (Array.Empty<byte>(), TimeSpan.Zero);

            try
            {
                var requestPayload = new RimeTtsRequest
                {
                    Text = text,
                    Speaker = _serviceConfig.Speaker,
                    ModelId = _serviceConfig.ModelId,
                    Lang = _serviceConfig.Lang,
                    SamplingRate = _selectedApiFormat.SampleRate,

                    RepetitionPenalty = _serviceConfig.ModelId == "arcana" ? _serviceConfig.RepetitionPenalty : null,
                    Temperature = _serviceConfig.ModelId == "arcana" ? _serviceConfig.Temperature : null,
                    TopP = _serviceConfig.ModelId == "arcana" ? _serviceConfig.TopP : null,
                    MaxTokens = _serviceConfig.ModelId == "arcana" ? _serviceConfig.MaxTokens : null,

                    SpeedAlpha = _serviceConfig.ModelId != "arcana" ? _serviceConfig.SpeedAlpha : null,
                    NoTextNormalization = _serviceConfig.ModelId != "arcana" ? _serviceConfig.NoTextNormalization : null,
                    PauseBetweenBrackets = _serviceConfig.ModelId != "arcana" ? _serviceConfig.PauseBetweenBrackets : null,
                    PhonemizeBetweenBrackets = _serviceConfig.ModelId != "arcana" ? _serviceConfig.PhonemizeBetweenBrackets : null,
                    InlineSpeedAlpha = _serviceConfig.ModelId != "arcana" ? _serviceConfig.InlineSpeedAlpha : null
                };

                string jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });

                using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/pcm"));
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Rime API Error {Code}: {Body}", response.StatusCode, errorContent);
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                byte[] sourceAudioData = await response.Content.ReadAsByteArrayAsync(cancellationToken);

                if (sourceAudioData.Length == 0)
                {
                    _logger.LogWarning("Rime returned 0 bytes.");
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                var duration = AudioConversationHelper.CalculateDuration(sourceAudioData, _optimalRimeFormat);

                if (_audioConversationNeeded)
                {
                    var (convertedData, _) = AudioConversationHelper.Convert(sourceAudioData, _optimalRimeFormat, _finalUserRequest, false);
                    return (convertedData, duration);
                }

                return (sourceAudioData, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rime Synthesis Error");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        public Task StopTextSynthesisAsync() => Task.CompletedTask;

        public string GetProviderFullName() => "RimeTextToSpeech";
        public InterfaceTTSProviderEnum GetProviderType() => GetProviderTypeStatic();
        public static InterfaceTTSProviderEnum GetProviderTypeStatic() => InterfaceTTSProviderEnum.RimeTextToSpeech;
        public ITTSConfig GetCacheableConfig() => _serviceConfig;

        public TTSProviderAvailableAudioFormat GetCurrentOutputFormat() => _optimalRimeFormat;

        public void Dispose() => GC.SuppressFinalize(this);

        // =================================================================================================
        // STATIC DATA & MAPPINGS
        // =================================================================================================

        private record RimeOutputFormatDefinition(int SampleRate);

        private static readonly ReadOnlyCollection<TTSProviderAvailableAudioFormat> RimeSupportedFormats;
        private static readonly ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), RimeOutputFormatDefinition> FormatMap;

        static RimeTTSService()
        {
            var supportedFormats = new List<TTSProviderAvailableAudioFormat>
            {
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 8000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 16000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 22050, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 24000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 44100, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 48000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 96000, BitsPerSample = 16 },
            };
            RimeSupportedFormats = supportedFormats.AsReadOnly();

            var formatMap = new Dictionary<(AudioEncodingTypeEnum, int, int), RimeOutputFormatDefinition>
            {
                { (AudioEncodingTypeEnum.PCM, 8000, 16), new(8000) },
                { (AudioEncodingTypeEnum.PCM, 16000, 16), new(16000) },
                { (AudioEncodingTypeEnum.PCM, 22050, 16), new(22050) },
                { (AudioEncodingTypeEnum.PCM, 24000, 16), new(24000) },
                { (AudioEncodingTypeEnum.PCM, 44100, 16), new(44100) },
                { (AudioEncodingTypeEnum.PCM, 48000, 16), new(48000) },
                { (AudioEncodingTypeEnum.PCM, 96000, 16), new(96000) },
            };
            FormatMap = new ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), RimeOutputFormatDefinition>(formatMap);
        }
    }
}