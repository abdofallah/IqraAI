using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS.Providers.HumeAI;
using IqraCore.Interfaces.AI;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class HumeAITTSService : ITTSService, IDisposable
    {
        private static readonly HttpClient _httpClient = new();
        private readonly string _apiKey;
        private readonly string? _voiceId;
        private readonly string? _voiceName;
        private readonly string? _voiceProvider;

        private const string ApiUrl = "https://api.hume.ai/v0/tts";

        public HumeAITTSService(string apiKey, string? voiceId = null, string? voiceName = null, string? voiceProvider = null)
        {
            _apiKey = apiKey;
            _voiceId = voiceId;
            _voiceName = voiceName;
            _voiceProvider = voiceProvider;
        }

        public void Initialize()
        {
            // Static HttpClient initialization is handled implicitly
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            HumeVoiceSpecifier? voiceSpec = null;
            if (!string.IsNullOrEmpty(_voiceId) || !string.IsNullOrEmpty(_voiceName))
            {
                voiceSpec = new HumeVoiceSpecifier
                {
                    Id = _voiceId,
                    Name = _voiceName,
                    Provider = _voiceProvider
                };
            }

            var utterance = new HumeUtteranceRequest
            {
                Text = text,
                Voice = voiceSpec
                // Could add description, speed, silence from metaData if desired
            };

            var requestPayload = new HumeTtsRequest
            {
                Utterances = new List<HumeUtteranceRequest> { utterance },
            };

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

                // --- Handling Output Format ---
                // The interface expects raw PCM. Hume might return MP3, WAV, or PCM.
                // If it returns WAV, we need to parse it like Fish Audio.
                // If it returns MP3, we either return MP3 bytes (breaking consistency)
                // or attempt transcoding (complex).
                // Let's check the response encoding format.
                string format = firstGeneration.Encoding?.Format?.ToLowerInvariant() ?? "unknown";
                int? sampleRate = firstGeneration.Encoding?.SampleRate;

                if (format == "wav")
                {
                    // todo logging
                    Console.WriteLine($"Hume AI: Received WAV format (SampleRate: {sampleRate}). Parsing header...");
                    // Use a WAV parser similar to the Fish Audio one
                    return ParseWavAndExtractPcm(audioData, duration); // Pass known duration
                }
                else if (format == "pcm")
                {
                    // todo logging
                    Console.WriteLine($"Hume AI: Received raw PCM format (SampleRate: {sampleRate}).");
                    // Assume it matches our 16-bit expectation or check metadata further if available
                    return (audioData, duration);
                }
                else if (format == "mp3")
                {
                    // todo logging
                    Console.WriteLine($"Hume AI: Received MP3 format (SampleRate: {sampleRate}). Returning MP3 bytes directly.");
                    // WARNING: This breaks the expectation of PCM from other providers.
                    // Consider logging this or adding transcoding if PCM is strictly required.
                    return (audioData, duration);
                }
                else
                {
                    // todo logging
                    Console.WriteLine($"Hume AI: Received unknown audio format '{format}'. Returning raw bytes.");
                    return (audioData, duration); // Return whatever bytes we got
                }
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

        // Simple WAV parser (reuse or adapt from FishAudio service)
        private (byte[]?, TimeSpan?) ParseWavAndExtractPcm(byte[] wavData, TimeSpan? knownDuration)
        {
            try
            {
                if (wavData == null || wavData.Length < 44) { return (null, null); }

                using var reader = new BinaryReader(new MemoryStream(wavData));
                // Basic WAV Header Parsing (adapt as needed from FishAudio implementation)
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "RIFF") return (wavData, knownDuration); // Return original if not RIFF
                reader.ReadInt32(); // ChunkSize
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "WAVE") return (wavData, knownDuration);
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "fmt ") return (wavData, knownDuration);
                int fmtChunkSize = reader.ReadInt32();
                reader.BaseStream.Seek(fmtChunkSize, SeekOrigin.Current); // Skip format chunk details for simplicity here

                // Find 'data' chunk
                string dataChunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                while (dataChunkId.ToLowerInvariant() != "data" && reader.BaseStream.Position < wavData.Length - 8)
                {
                    int listChunkSize = reader.ReadInt32();
                    reader.BaseStream.Seek(listChunkSize, SeekOrigin.Current);
                    dataChunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                }
                if (dataChunkId.ToLowerInvariant() != "data") return (wavData, knownDuration); // Return original if no data chunk

                int dataChunkSize = reader.ReadInt32();
                byte[] pcmData = reader.ReadBytes(dataChunkSize);

                // Return extracted PCM data; keep the duration from the API response
                return (pcmData, knownDuration);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hume AI WAV Parsing Error: {ex.Message}. Returning original WAV data.");
                // Return original data if parsing fails, use API duration
                return (wavData, knownDuration);
            }
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

        public void Dispose()
        {
            // Static HttpClient doesn't need instance disposal
            GC.SuppressFinalize(this);
        }
    }
}