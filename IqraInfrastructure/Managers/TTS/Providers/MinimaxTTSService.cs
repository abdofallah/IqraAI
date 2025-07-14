using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS.Providers.Minimax;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.TTS;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class MinimaxTTSService : ITTSService, IDisposable
    {
        private readonly string _apiKey;
        private readonly string _groupId;
        private readonly MinimaxConfig _serviceConfig;
        
        // Hardcoded values based on MiniMax's requirements
        private readonly int _bytesPerSample = 2;
        private readonly int _channels = 1;
        private const string BaseUrl = "https://api.minimaxi.chat/v1";

        private static readonly HttpClient _httpClient = new();

        public MinimaxTTSService(string apiKey, string groupId, MinimaxConfig config)
        {
            _apiKey = apiKey;
            _groupId = groupId;
            _serviceConfig = config;
        }

        public void Initialize()
        {
            // Static HttpClient initialization is handled implicitly

            if (!(new List<int>([8000, 16000, 22050, 24000, 32000, 44100])).Contains(_serviceConfig.TargetSampleRate))
            {
                throw new Exception("Sample rate support are 8000, 16000, 22050, 24000, 32000 or 44100");
            }
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            var requestPayload = new MinimaxTtsRequest
            {
                Model = _serviceConfig.ModelId,
                Text = text,
                Stream = false,
                OutputFormat = "hex",
                VoiceSetting = new MinimaxVoiceSetting {
                    VoiceId = _serviceConfig.VoiceId,
                    Speed = _serviceConfig.VoiceSpeed
                },
                AudioSetting = new MinimaxAudioSetting
                {
                    Format = "pcm",
                    SampleRate = _serviceConfig.TargetSampleRate,
                    Channel = _channels,
                    Bitrate = (_serviceConfig.TargetSampleRate * (_bytesPerSample * 8))
                },
                SubtitleEnable = false,
                PronunciationDict = _serviceConfig.PronunciationDict,
                LanguageBoost = _serviceConfig.LanguageBoostId
            };

            string jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            var requestUri = $"{BaseUrl}/t2a_v2?GroupId={_groupId}";

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);

                string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    // todo logging
                    Console.WriteLine($"MiniMax HTTP Error ({response.StatusCode}): {responseBody}");
                    // Attempt to parse base_resp even on HTTP error if possible
                    try
                    {
                        var errorResp = JsonSerializer.Deserialize<MinimaxTtsResponse>(responseBody);
                        if (errorResp?.BaseResp != null)
                        {
                            // todo logging
                            Console.WriteLine($"MiniMax API Error: Code={errorResp.BaseResp.StatusCode}, Msg='{errorResp.BaseResp.StatusMsg}'");
                        }
                    }
                    catch { /* Ignore deserialize error on error path */ }
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                // Deserialize successful response
                var ttsResponse = JsonSerializer.Deserialize<MinimaxTtsResponse>(responseBody);

                // Check API-level success code
                if (ttsResponse?.BaseResp?.StatusCode != 0)
                {
                    // todo logging
                    Console.WriteLine($"MiniMax API Error: Code={ttsResponse.BaseResp.StatusCode}, Msg='{ttsResponse.BaseResp.StatusMsg}'");
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                // Check if audio data exists
                if (string.IsNullOrWhiteSpace(ttsResponse.Data?.Audio))
                {
                    // todo logging
                    Console.WriteLine("MiniMax TTS Error: API success but no audio data received.");
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                // Decode the hex audio string
                byte[] audioData = ConvertHexStringToByteArray(ttsResponse.Data.Audio);
                TimeSpan? duration = null;

                // Get duration from extra_info if available
                if (ttsResponse.ExtraInfo != null && ttsResponse.ExtraInfo.AudioLength > 0)
                {
                    duration = TimeSpan.FromMilliseconds(ttsResponse.ExtraInfo.AudioLength);
                }
                else
                {
                    // TODO make sure no constant is used

                    // Fallback: Calculate duration manually if ExtraInfo isn't present/valid
                    // This requires knowing the BytesPerSample and Channels from the *response* (ExtraInfo)
                    // or assuming they match the request (less reliable).
                    int bytesPerSample = _bytesPerSample; // Assuming PCM 16-bit based on common practice
                    int channels = _channels; // Use response channel if available, else assume 1
                    int sampleRate = _serviceConfig.TargetSampleRate; // Use response rate if available

                    if (sampleRate > 0 && bytesPerSample > 0 && channels > 0 && audioData.Length > 0)
                    {
                        double durationSeconds = (double)audioData.Length / (sampleRate * channels * bytesPerSample);
                        duration = TimeSpan.FromSeconds(durationSeconds);
                    }
                }

                return (audioData, duration);

            }
            catch (JsonException jsonEx)
            {
                // todo logging
                Console.WriteLine($"MiniMax JSON Deserialization Error: {jsonEx.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (FormatException formatEx) // From hex decoding
            {
                // todo logging
                Console.WriteLine($"MiniMax Hex Decoding Error: {formatEx.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (HttpRequestException httpEx)
            {
                // todo logging
                Console.WriteLine($"MiniMax HTTP Request Error: {httpEx.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                // todo logging
                Console.WriteLine("MiniMax TTS synthesis was cancelled.");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                // todo logging
                Console.WriteLine($"MiniMax TTS Error: {ex.GetType().Name} - {ex.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        // Helper function to decode hex string
        private static byte[] ConvertHexStringToByteArray(string hexString)
        {
            if (hexString.Length % 2 != 0)
            {
                throw new FormatException("Hex string must have an even number of characters.");
            }

            return Convert.FromHexString(hexString);
        }

        public Task StopTextSynthesisAsync()
        {
            // Cancellation is handled via the CancellationToken passed to SynthesizeTextAsync
            return Task.CompletedTask;
        }

        public string GetProviderFullName()
        {
            return "MinimaxTextToSpeech";
        }

        public InterfaceTTSProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public static InterfaceTTSProviderEnum GetProviderTypeStatic()
        {
            return InterfaceTTSProviderEnum.MinimaxTextToSpeech;
        }

        public ITtsConfig GetCacheableConfig()
        {
            return _serviceConfig;
        }
        public void Dispose()
        {
            // Static HttpClient doesn't need instance disposal
            GC.SuppressFinalize(this);
        }
    }
}