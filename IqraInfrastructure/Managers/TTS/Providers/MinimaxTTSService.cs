using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraCore.Entities.TTS.Providers.Minimax;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.TTS;
using IqraInfrastructure.Helpers.Audio;
using IqraInfrastructure.Managers.TTS.Helpers;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class MinimaxTTSService : ITTSService, IDisposable
    {
        private static readonly HttpClient _httpClient = new();
        private readonly ILogger<MinimaxTTSService> _logger;
        private readonly string _apiKey;
        private readonly MinimaxConfig _serviceConfig;

        private const string BaseUrl = "https://api.minimax.io/v1/t2a_v2";

        // State
        private AudioRequestDetails _finalUserRequest;
        private TTSProviderAvailableAudioFormat _optimalMiniMaxFormat;
        private MiniMaxOutputFormatDefinition _selectedApiFormat;
        private bool _audioConversationNeeded = false;

        public MinimaxTTSService(ILogger<MinimaxTTSService> logger, string apiKey, MinimaxConfig config)
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

                var bestFallbackOrder = AudiEncoderFallbackSelector.GetFallbackOrder(_finalUserRequest, MiniMaxSupportedFormats);
                _optimalMiniMaxFormat = bestFallbackOrder.FirstOrDefault();

                if (_optimalMiniMaxFormat == null)
                {
                    return result.SetFailureResult(
                        "Initialize:FORMAT_NOT_SUPPORTED",
                        $"MiniMax TTS does not support a format compatible with: {_finalUserRequest.RequestedEncoding} @ {_finalUserRequest.RequestedSampleRateHz}Hz"
                    );
                }

                var formatKey = (_optimalMiniMaxFormat.Encoding, _optimalMiniMaxFormat.SampleRateHz, _optimalMiniMaxFormat.BitsPerSample);
                if (!FormatMap.TryGetValue(formatKey, out _selectedApiFormat))
                {
                    throw new InvalidOperationException($"Internal error: No mapping found for selected format: {formatKey}");
                }

                _audioConversationNeeded = _optimalMiniMaxFormat.Encoding != _finalUserRequest.RequestedEncoding ||
                                           _optimalMiniMaxFormat.SampleRateHz != _finalUserRequest.RequestedSampleRateHz ||
                                           _optimalMiniMaxFormat.BitsPerSample != _finalUserRequest.RequestedBitsPerSample;

                // 5. Account Check
                var accountCheck = await CheckAccount();
                if (!accountCheck.Success)
                {
                    return result.SetFailureResult(accountCheck.Code, accountCheck.Message);
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("Initialize:EXCEPTION", $"MiniMax init error: {ex.Message}");
            }
        }

        public async Task<FunctionReturnResult> CheckAccount()
        {
            // MiniMax doesn't have a dedicated lightweight auth check endpoint.
            // We assume valid config until first request failure.
            return await Task.FromResult(new FunctionReturnResult().SetSuccessResult());
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            if (string.IsNullOrEmpty(text)) return (Array.Empty<byte>(), TimeSpan.Zero);

            try
            {
                var requestPayload = new MinimaxTtsRequest
                {
                    Model = _serviceConfig.ModelId,
                    Text = text,
                    Stream = false,
                    LanguageBoost = _serviceConfig.LanguageBoost,
                    OutputFormat = "hex",
                    VoiceSetting = new MinimaxVoiceSetting
                    {
                        VoiceId = _serviceConfig.VoiceId,
                        Speed = _serviceConfig.VoiceSpeed,
                        Vol = _serviceConfig.VoiceVolume,
                        Pitch = _serviceConfig.VoicePitch,
                        Emotion = _serviceConfig.VoiceEmotions,
                        TextNormalization = _serviceConfig.VoiceTextNormalization,
                        LatexRead = _serviceConfig.VoiceLatexRead
                    },
                    PronunciationDict = _serviceConfig.PronunciationDict,
                    AudioSetting = new MinimaxAudioSetting
                    {
                        SampleRate = _selectedApiFormat.SampleRate,
                        Format = _selectedApiFormat.FormatString,
                        Channel = 1
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });

                using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("MiniMax API Error {StatusCode}: {Content}", response.StatusCode, errorContent);
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                var contentType = response.Content.Headers.ContentType?.MediaType;

                var responseData = await response.Content.ReadFromJsonAsync<MinimaxResponseData>(cancellationToken);
                if (responseData == null)
                {
                    _logger.LogError("Unable to deserialize MiniMax response");
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                if (responseData.BaseResp.StatusMsg != "success" && responseData.BaseResp.StatusCode != 0)
                {
                    _logger.LogError("MiniMax API returned error: {code} {message}", responseData.BaseResp.StatusCode, responseData.BaseResp.StatusMsg);
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                if (responseData.Data.Status != 2)
                {
                    _logger.LogError("MiniMax API audio status came as incomplete: {code}", responseData.Data.Status);
                }

                var sourceAudioData = Convert.FromHexString(responseData.Data.Audio);

                var duration = AudioConversationHelper.CalculateDuration(sourceAudioData, _optimalMiniMaxFormat);

                if (_audioConversationNeeded)
                {
                    var (convertedData, _) = AudioConversationHelper.Convert(sourceAudioData, _optimalMiniMaxFormat, _finalUserRequest, false);
                    return (convertedData, duration);
                }

                return (sourceAudioData, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MiniMax Synthesis Error");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        public Task StopTextSynthesisAsync() => Task.CompletedTask;

        public string GetProviderFullName() => "MiniMaxTextToSpeech";
        public InterfaceTTSProviderEnum GetProviderType() => GetProviderTypeStatic();
        public static InterfaceTTSProviderEnum GetProviderTypeStatic() => InterfaceTTSProviderEnum.MinimaxTextToSpeech;
        public ITTSConfig GetCacheableConfig() => _serviceConfig;

        public TTSProviderAvailableAudioFormat GetCurrentOutputFormat() => _optimalMiniMaxFormat;

        public void Dispose() => GC.SuppressFinalize(this);

        // =================================================================================================
        // STATIC DATA & MAPPINGS
        // =================================================================================================

        private record MiniMaxOutputFormatDefinition(string FormatString, int SampleRate, int BitsPerSample);

        private static readonly ReadOnlyCollection<TTSProviderAvailableAudioFormat> MiniMaxSupportedFormats;
        private static readonly ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), MiniMaxOutputFormatDefinition> FormatMap;

        static MinimaxTTSService()
        {
            var supportedFormats = new List<TTSProviderAvailableAudioFormat>
            {
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 8000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 16000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 22050, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 24000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 32000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 44100, BitsPerSample = 16 },
            };
            MiniMaxSupportedFormats = supportedFormats.AsReadOnly();

            var formatMap = new Dictionary<(AudioEncodingTypeEnum, int, int), MiniMaxOutputFormatDefinition>
            {
                { (AudioEncodingTypeEnum.PCM, 8000, 16), new("pcm", 8000, 16) },
                { (AudioEncodingTypeEnum.PCM, 16000, 16), new("pcm", 16000, 16) },
                { (AudioEncodingTypeEnum.PCM, 22050, 16), new("pcm", 22050, 16) },
                { (AudioEncodingTypeEnum.PCM, 24000, 16), new("pcm", 24000, 16) },
                { (AudioEncodingTypeEnum.PCM, 32000, 16), new("pcm", 32000, 16) },
                { (AudioEncodingTypeEnum.PCM, 44100, 16), new("pcm", 44100, 16) },
            };
            FormatMap = new ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), MiniMaxOutputFormatDefinition>(formatMap);
        }
    }
}