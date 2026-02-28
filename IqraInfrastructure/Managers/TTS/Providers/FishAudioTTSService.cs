using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraCore.Entities.TTS.Providers.FishAudio;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.TTS;
using IqraInfrastructure.Helpers.Audio;
using IqraInfrastructure.Managers.TTS.Helpers;
using MessagePack;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class FishAudioTTSService : ITTSService, IDisposable
    {
        private static readonly HttpClient _httpClient = new();
        private readonly string _apiKey;
        private readonly FishAudioConfig _serviceConfig;

        private const string BaseUrl = "https://api.fish.audio";

        // State
        private AudioRequestDetails _finalUserRequest;
        private TTSProviderAvailableAudioFormat _optimalFishFormat;
        private FishAudioOutputFormatDefinition _selectedApiFormat;
        private bool _audioConversationNeeded = false;

        public FishAudioTTSService(string apiKey, FishAudioConfig config)
        {
            _apiKey = apiKey;
            _serviceConfig = config;
        }

        public async Task<FunctionReturnResult> Initialize()
        {
            var result = new FunctionReturnResult();

            try
            {
                // 1. Prepare Request Details
                _finalUserRequest = new AudioRequestDetails
                {
                    RequestedEncoding = _serviceConfig.TargetEncodingType,
                    RequestedSampleRateHz = _serviceConfig.TargetSampleRate,
                    RequestedBitsPerSample = _serviceConfig.TargetBitsPerSample
                };

                // 2. Select Optimal Format
                var bestFallbackOrder = AudiEncoderFallbackSelector.GetFallbackOrder(_finalUserRequest, FishSupportedFormats);
                _optimalFishFormat = bestFallbackOrder.FirstOrDefault();

                if (_optimalFishFormat == null)
                {
                    return result.SetFailureResult(
                        "Initialize:FORMAT_NOT_SUPPORTED",
                        $"FishAudio TTS does not support a format compatible with: {_finalUserRequest.RequestedEncoding} @ {_finalUserRequest.RequestedSampleRateHz}Hz"
                    );
                }

                // 3. Map to API Definition
                var formatKey = (_optimalFishFormat.Encoding, _optimalFishFormat.SampleRateHz, _optimalFishFormat.BitsPerSample);
                if (!FormatMap.TryGetValue(formatKey, out _selectedApiFormat))
                {
                    throw new InvalidOperationException($"Internal error: No mapping found for selected format: {formatKey}");
                }

                // 4. Determine Conversion Needs
                _audioConversationNeeded = _optimalFishFormat.Encoding != _finalUserRequest.RequestedEncoding ||
                                           _optimalFishFormat.SampleRateHz != _finalUserRequest.RequestedSampleRateHz ||
                                           _optimalFishFormat.BitsPerSample != _finalUserRequest.RequestedBitsPerSample;

                // 5. Check Account
                var accountCheck = await CheckAccount();
                if (!accountCheck.Success)
                {
                    return result.SetFailureResult(accountCheck.Code, accountCheck.Message);
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("Initialize:EXCEPTION", $"FishAudio init error: {ex.Message}");
            }
        }

        public async Task<FunctionReturnResult> CheckAccount()
        {
            var result = new FunctionReturnResult();
            try
            {
                // Check wallet/self to get credit balance
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/v1/wallet/self/package");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                using var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return result.SetFailureResult("CheckAccount:INVALID_KEY", "FishAudio API Key is invalid.");
                    }
                    return result.SetFailureResult("CheckAccount:API_ERROR", $"FishAudio Account Check Failed: {response.StatusCode}");
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                var jsonNode = JsonNode.Parse(jsonContent);

                if (jsonNode == null)
                {
                    return result.SetFailureResult("CheckAccount:INVALID_RESPONSE", "Empty response from FishAudio.");
                }

                // Parse credits (handle standard JSON numeric types)
                decimal balanceCredit = (decimal?)jsonNode["balance"] ?? 0m;
                decimal extraBalanceCredit = (decimal?)jsonNode["extra_balance"] ?? 0m;

                if (balanceCredit > 0)
                {
                    return result.SetSuccessResult();
                }

                if (extraBalanceCredit > 0)
                {
                    return result.SetSuccessResult();
                }

                return result.SetFailureResult(
                    "CheckAccount:INSUFFICIENT_FUNDS",
                    $"Insufficient funds. Balance: {balanceCredit}, Extra Balance: {extraBalanceCredit}"
                );
            }
            catch (Exception ex)
            {
                // If the endpoint changes or parsing fails, we default to allowing it to avoid blocking valid calls due to API updates.
                // But we log the specific error in the failure message for debugging.
                return result.SetFailureResult("CheckAccount:EXCEPTION", $"Failed to validate FishAudio Account: {ex.Message}");
            }
        }


        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            if (string.IsNullOrEmpty(text)) return (Array.Empty<byte>(), TimeSpan.Zero);

            FishAudioProsody? prosody = null;
            if (_serviceConfig.Speed.HasValue || _serviceConfig.Volume.HasValue)
            {
                prosody = new FishAudioProsody
                {
                    Speed = _serviceConfig.Speed ?? 1.0f,
                    Volume = _serviceConfig.Volume ?? 0.0f
                };
            }

            var requestPayload = new FishAudioTTSRequest
            {
                Text = text,
                ReferenceId = _serviceConfig.ReferenceId,
                Format = _selectedApiFormat.FormatString,
                SampleRate = _selectedApiFormat.SampleRateHz,

                // Optional Parameters
                Temperature = _serviceConfig.Temperature,
                TopP = _serviceConfig.TopP,
                Prosody = prosody,
                Latency = _serviceConfig.Latency,
                Normalize = _serviceConfig.Normalize,
                RepetitionPenalty = _serviceConfig.RepetitionPenalty,
                ChunkLength = _serviceConfig.ChunkLength,
                MaxNewTokens = _serviceConfig.MaxNewTokens
            };

            byte[] messagePackData = MessagePackSerializer.Serialize(requestPayload);

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v1/tts");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/wav")); // Even for PCM
            request.Headers.Add("model", _serviceConfig.Model);

            request.Content = new ByteArrayContent(messagePackData);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/msgpack");

            try
            {
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    // todo add logger
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                byte[] sourceAudioData = await response.Content.ReadAsByteArrayAsync(cancellationToken);

                // Calculate Duration
                var duration = AudioConversationHelper.CalculateDuration(sourceAudioData, _optimalFishFormat);

                // Post-Process
                if (_audioConversationNeeded)
                {
                    var (convertedData, _) = AudioConversationHelper.Convert(sourceAudioData, _optimalFishFormat, _finalUserRequest, false);
                    return (convertedData, duration);
                }

                return (sourceAudioData, duration);
            }
            catch (Exception)
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        public Task StopTextSynthesisAsync() => Task.CompletedTask;

        public string GetProviderFullName() => "FishAudioTextToSpeech";
        public InterfaceTTSProviderEnum GetProviderType() => GetProviderTypeStatic();
        public static InterfaceTTSProviderEnum GetProviderTypeStatic() => InterfaceTTSProviderEnum.FishAudioTextToSpeech;
        public ITTSConfig GetCacheableConfig() => _serviceConfig;

        public TTSProviderAvailableAudioFormat GetCurrentOutputFormat() => _optimalFishFormat;

        public void Dispose() => GC.SuppressFinalize(this);

        // =================================================================================================
        // STATIC DATA & MAPPINGS
        // =================================================================================================

        private record FishAudioOutputFormatDefinition(string FormatString, int SampleRateHz, int BitsPerSample);

        private static readonly ReadOnlyCollection<TTSProviderAvailableAudioFormat> FishSupportedFormats;
        private static readonly ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), FishAudioOutputFormatDefinition> FormatMap;

        static FishAudioTTSService()
        {
            var supportedFormats = new List<TTSProviderAvailableAudioFormat>
            {
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 8000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 16000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 24000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 32000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 44100, BitsPerSample = 16 },
            };
            FishSupportedFormats = supportedFormats.AsReadOnly();

            var formatMap = new Dictionary<(AudioEncodingTypeEnum, int, int), FishAudioOutputFormatDefinition>
            {
                { (AudioEncodingTypeEnum.PCM, 8000, 16), new("pcm", 8000, 16) },
                { (AudioEncodingTypeEnum.PCM, 16000, 16), new("pcm", 16000, 16) },
                { (AudioEncodingTypeEnum.PCM, 24000, 16), new("pcm", 24000, 16) },
                { (AudioEncodingTypeEnum.PCM, 32000, 16), new("pcm", 32000, 16) },
                { (AudioEncodingTypeEnum.PCM, 44100, 16), new("pcm", 44100, 16) },
            };
            FormatMap = new ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), FishAudioOutputFormatDefinition>(formatMap);
        }
    }
}