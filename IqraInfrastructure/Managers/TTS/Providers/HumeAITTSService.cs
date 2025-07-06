using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS.Providers.HumeAI;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.TTS;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class HumeAITTSService : ITTSService, IDisposable
    {
        private readonly string _apiKey;
        private readonly HumeAiConfig _serviceConfig;

        private static readonly HttpClient _httpClient = new();

        private const string ApiUrl = "https://api.hume.ai/v0/tts";

        // Hardcoded values based on Hume AI's requirements
        private readonly int _sampleSize = 16;
        private readonly int _channels = 1; 

        private string lastGenerationId = null;

        public HumeAITTSService(string apiKey, HumeAiConfig config)
        {
            _apiKey = apiKey;
            _serviceConfig = config;
        }

        public void Initialize()
        {
            // Static HttpClient initialization is handled implicitly
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            var voiceSpec = new HumeVoiceSpecifier
            {
                Id = _serviceConfig.VoiceId,
                Provider = _serviceConfig.VoiceProvider
            };

            var utterance = new HumeUtteranceRequest
            {
                Text = text,
                Voice = voiceSpec,
                Description = _serviceConfig.VoiceDescription,
                Speed = _serviceConfig.VoiceSpeed
            };

            var requestPayload = new HumeTtsRequest
            {
                Utterances = new List<HumeUtteranceRequest> {
                    utterance
                },
                AudioFormat = new HumeTtsRequestAudioFormat() {
                    Type = "pcm"
                }
            };

            if (!string.IsNullOrWhiteSpace(lastGenerationId))
            {
                requestPayload.Context = new HumeTtsRequestContext()
                {
                    GenerationId = lastGenerationId
                };
            }

            string jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            var requestUri = ApiUrl;

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Add("X-Hume-Api-Key", _apiKey);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);

                string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    // todo logging
                    Console.WriteLine($"Hume AI HTTP Error ({response.StatusCode}): {responseBody}");
                    // Attempt to parse standard Hume error format
                    try
                    {
                        var errorResp = JsonSerializer.Deserialize<HumeTtsResponse>(responseBody);
                        if (!string.IsNullOrEmpty(errorResp?.ErrorCode) || !string.IsNullOrEmpty(errorResp?.ErrorMessage))
                        {
                            // todo logging
                            Console.WriteLine($"Hume AI API Error: Code={errorResp.ErrorCode}, Msg='{errorResp.ErrorMessage}'");
                        }
                    }
                    catch { /* Ignore deserialize error on error path */ }
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                var ttsResponse = JsonSerializer.Deserialize<HumeTtsResponse>(responseBody);

                // Check for API level error even on 200 OK
                if (!string.IsNullOrEmpty(ttsResponse?.ErrorCode) || !string.IsNullOrEmpty(ttsResponse?.ErrorMessage))
                {
                    // todo logging
                    Console.WriteLine($"Hume AI API Error: Code={ttsResponse.ErrorCode}, Msg='{ttsResponse.ErrorMessage}'");
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                if (ttsResponse?.Generations == null || !ttsResponse.Generations.Any())
                {
                    // todo logging
                    Console.WriteLine("Hume AI TTS Error: No generations returned in the response.");
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                var firstGeneration = ttsResponse.Generations[0];

                if (string.IsNullOrWhiteSpace(firstGeneration.Audio))
                {
                    // todo logging
                    Console.WriteLine("Hume AI TTS Error: First generation contains no audio data.");
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                // Decode Base64 audio
                byte[] audioData = Convert.FromBase64String(firstGeneration.Audio);
                TimeSpan? duration = null;

                if (firstGeneration.Duration.HasValue)
                {
                    duration = TimeSpan.FromSeconds(firstGeneration.Duration.Value);
                }
                
                if (!string.IsNullOrWhiteSpace(firstGeneration.GenerationId))
                {
                    lastGenerationId = firstGeneration.GenerationId;
                }

                int actualSampleRate = 48000;         
                if (firstGeneration.Encoding != null) // Optionally, check firstGeneration.Encoding if available and trust it more:
                {
                    if (firstGeneration.Encoding.SampleRate.HasValue)
                        actualSampleRate = firstGeneration.Encoding.SampleRate.Value;
                }

                byte[] resampledAudioData = ResamplePcm(audioData, actualSampleRate, _serviceConfig.SampleRate, _channels, _sampleSize);

                return (resampledAudioData, duration);
            }
            catch (JsonException jsonEx)
            {
                // todo logging
                Console.WriteLine($"Hume AI JSON Deserialization Error: {jsonEx.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (FormatException formatEx) // From Base64 decoding
            {
                // todo logging
                Console.WriteLine($"Hume AI Base64 Decoding Error: {formatEx.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (HttpRequestException httpEx)
            {
                // todo logging
                Console.WriteLine($"Hume AI HTTP Request Error: {httpEx.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                // todo logging
                Console.WriteLine("Hume AI TTS synthesis was cancelled.");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                // todo logging
                Console.WriteLine($"Hume AI TTS Error: {ex.GetType().Name} - {ex.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        private static short[] PcmBytesToShorts(byte[] pcmData)
        {
            if (pcmData.Length % 2 != 0) return Array.Empty<short>();
            short[] samples = new short[pcmData.Length / 2];
            Buffer.BlockCopy(pcmData, 0, samples, 0, pcmData.Length);
            return samples;
        }

        private static byte[] PcmShortsToBytes(short[] samples)
        {
            byte[] pcmData = new byte[samples.Length * 2];
            Buffer.BlockCopy(samples, 0, pcmData, 0, pcmData.Length);
            return pcmData;
        }

        private byte[] ResamplePcm(byte[] pcmData, int originalSampleRate, int targetSampleRate, int numChannels, int bitsPerSample)
        {
            if (originalSampleRate == targetSampleRate || pcmData.Length == 0)
            {
                return pcmData;
            }

            if (bitsPerSample != 16 || numChannels <= 0 || originalSampleRate <= 0)
            {
                // todo logging
                Console.WriteLine($"Hume AI Resample: Unsupported PCM format for resampling (Bits: {bitsPerSample}, Channels: {numChannels}, OriginalRate: {originalSampleRate}). Returning original data.");
                return pcmData;
            }

            short[] inputShorts = PcmBytesToShorts(pcmData);
            if (inputShorts.Length == 0 && pcmData.Length > 0)
            {
                // todo logging
                Console.WriteLine("Hume AI Resample: Failed to convert PCM bytes to shorts. Returning original data.");
                return pcmData;
            }


            int inputFrames = inputShorts.Length / numChannels;
            if (inputFrames == 0 && inputShorts.Length > 0)
            {
                // todo logging
                Console.WriteLine("Hume AI Resample: Not enough samples for even one frame. Returning original data.");
                return pcmData;
            }


            int outputFrames = (int)Math.Max(1, Math.Round(inputFrames * (double)targetSampleRate / originalSampleRate));
            short[] outputShorts = new short[outputFrames * numChannels];

            double step = (double)originalSampleRate / targetSampleRate;

            for (int i = 0; i < outputFrames; i++)
            {
                double originalFrameIndexDouble = i * step;

                for (int c = 0; c < numChannels; c++)
                {
                    int baseInputFrameFloor = (int)Math.Floor(originalFrameIndexDouble);
                    int inputIndex1 = (baseInputFrameFloor * numChannels) + c;

                    if (inputIndex1 < 0) inputIndex1 = c;
                    if (inputIndex1 >= inputShorts.Length) inputIndex1 = Math.Max(0, inputShorts.Length - numChannels + c);
                    if (inputIndex1 < 0 && inputShorts.Length > 0) inputIndex1 = 0;


                    short sample1 = (inputShorts.Length > 0 && inputIndex1 < inputShorts.Length) ? inputShorts[inputIndex1] : (short)0;

                    if (originalSampleRate < targetSampleRate) // Upsampling
                    {
                        int inputIndex2 = ((baseInputFrameFloor + 1) * numChannels) + c;
                        if (inputIndex2 >= inputShorts.Length) inputIndex2 = inputIndex1;

                        short sample2 = (inputShorts.Length > 0 && inputIndex2 < inputShorts.Length) ? inputShorts[inputIndex2] : sample1;
                        double fraction = originalFrameIndexDouble - baseInputFrameFloor;
                        outputShorts[i * numChannels + c] = (short)(sample1 * (1.0 - fraction) + sample2 * fraction);
                    }
                    else // Downsampling (or no change, handled by outer if) - nearest neighbor
                    {
                        outputShorts[i * numChannels + c] = sample1;
                    }
                }
            }
            return PcmShortsToBytes(outputShorts);
        }

        public Task StopTextSynthesisAsync()
        {
            // Cancellation is handled via the CancellationToken passed to SynthesizeTextAsync
            return Task.CompletedTask;
        }

        public string GetProviderFullName()
        {
            return "HumeAITextToSpeech";
        }

        public InterfaceTTSProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public static InterfaceTTSProviderEnum GetProviderTypeStatic()
        {
            return InterfaceTTSProviderEnum.HumeAITextToSpeech;
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