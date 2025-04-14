using IqraCore.Entities.TTS.Providers.MurfAI;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class MurfAITTSService : ITTSService, IDisposable
    {
        private static readonly HttpClient _httpClient = new();
        private readonly string _apiKey;
        private readonly string _voiceId;
        private readonly int _sampleRate = 16000; // Define target sample rate

        private const string ApiUrl = "https://api.murf.ai/v1/speech/generate";

        public MurfAITTSService(string apiKey, string voiceId)
        {
            _apiKey = apiKey;
            _voiceId = voiceId;
        }

        public void Initialize()
        {
            // Static HttpClient initialization is handled implicitly
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            if (string.IsNullOrEmpty(text))
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }

            var requestPayload = new MurfTtsGenerateRequest
            {
                Text = text,
                VoiceId = _voiceId,
                Format = "WAV",           // Request WAV
                SampleRate = _sampleRate,   // Request desired sample rate
                EncodeAsBase64 = true,     // Request Base64 embedded audio
                ChannelType = "MONO",     // Request Mono
                ModelVersion = "GEN2"       // Use newer model
                // Populate optional fields from metaData if needed (e.g., pitch, rate)
                // Pitch = metaData?.TryGetValue("pitch", out var pitchVal) ? (int?)pitchVal : null,
            };

            string jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            var requestUri = ApiUrl;

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Add("api-key", _apiKey); // Use api-key header
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")); // Expect JSON response

            try
            {
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);

                string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Murf AI HTTP Error ({response.StatusCode}): {responseBody}");
                    // Attempt to parse error details from body
                    try
                    {
                        var errorResp = JsonSerializer.Deserialize<MurfTtsGenerateResponse>(responseBody);
                        if (errorResp?.ErrorCode != null || !string.IsNullOrEmpty(errorResp?.ErrorMessage))
                        {
                            // todo logging
                            Console.WriteLine($"Murf AI API Error: Code={errorResp.ErrorCode}, Msg='{errorResp.ErrorMessage}'");
                        }
                    }
                    catch { /* Ignore deserialize error on error path */ }
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                var ttsResponse = JsonSerializer.Deserialize<MurfTtsGenerateResponse>(responseBody);

                // Check for API level error message even on 200 OK
                if (ttsResponse?.ErrorCode != null || !string.IsNullOrEmpty(ttsResponse?.ErrorMessage))
                {
                    // todo logging
                    Console.WriteLine($"Murf AI API Error: Code={ttsResponse.ErrorCode}, Msg='{ttsResponse.ErrorMessage}'");
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                if (string.IsNullOrWhiteSpace(ttsResponse?.EncodedAudio))
                {
                    // todo logging
                    Console.WriteLine("Murf AI TTS Error: API success but no encoded audio data received.");
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                // Decode Base64 audio
                byte[] audioData = Convert.FromBase64String(ttsResponse.EncodedAudio);
                TimeSpan? duration = null;

                // Get duration from response if available
                if (ttsResponse.AudioLengthInSeconds.HasValue && ttsResponse.AudioLengthInSeconds > 0)
                {
                    duration = TimeSpan.FromSeconds(ttsResponse.AudioLengthInSeconds.Value);
                }

                // Since we requested WAV, parse it to get raw PCM and potentially recalculate duration if needed
                return ParseWavAndExtractPcm(audioData, duration); // Pass known duration if available

            }
            catch (JsonException jsonEx)
            {
                // todo logging
                Console.WriteLine($"Murf AI JSON Deserialization Error: {jsonEx.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (FormatException formatEx) // From Base64 decoding
            {
                // todo logging
                Console.WriteLine($"Murf AI Base64 Decoding Error: {formatEx.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (HttpRequestException httpEx)
            {
                // todo logging
                Console.WriteLine($"Murf AI HTTP Request Error: {httpEx.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                // todo logging
                Console.WriteLine("Murf AI TTS synthesis was cancelled.");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                // todo logging
                Console.WriteLine($"Murf AI TTS Error: {ex.GetType().Name} - {ex.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        // WAV parser (reuse or adapt from other services)
        private (byte[]?, TimeSpan?) ParseWavAndExtractPcm(byte[] wavData, TimeSpan? knownDuration)
        {
            try
            {
                if (wavData == null || wavData.Length < 44) { return (null, null); }

                using var reader = new BinaryReader(new MemoryStream(wavData));
                // --- Basic WAV Header Parsing ---
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "RIFF") return (wavData, knownDuration);
                reader.ReadInt32(); // File size - 8
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "WAVE") return (wavData, knownDuration);
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "fmt ") return (wavData, knownDuration);

                int fmtChunkSize = reader.ReadInt32();
                short audioFormat = reader.ReadInt16();
                short channels = reader.ReadInt16();
                int sampleRate = reader.ReadInt32();
                reader.ReadInt32(); // Byte rate
                reader.ReadInt16(); // Block align
                short bitsPerSample = reader.ReadInt16();

                if (audioFormat != 1)
                {
                    // todo logging
                    Console.WriteLine("Murf AI Error: Received WAV is not in PCM format.");
                    return (wavData, knownDuration);
                }
                if (fmtChunkSize > 16)
                    reader.BaseStream.Seek(fmtChunkSize - 16, SeekOrigin.Current);

                string dataChunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                while (dataChunkId.ToLowerInvariant() != "data" && reader.BaseStream.Position < wavData.Length - 8)
                {
                    int listChunkSize = reader.ReadInt32();
                    if (listChunkSize <= 0 || reader.BaseStream.Position + listChunkSize > wavData.Length)
                        return (wavData, knownDuration);
                    reader.BaseStream.Seek(listChunkSize, SeekOrigin.Current);
                    dataChunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                }
                if (dataChunkId.ToLowerInvariant() != "data")
                {
                    // todo logging
                    Console.WriteLine("Murf AI Error: 'data' chunk not found in WAV.");
                    return (wavData, knownDuration);
                }

                int dataChunkSize = reader.ReadInt32();
                if (reader.BaseStream.Position + dataChunkSize > wavData.Length)
                {
                    // todo logging
                    Console.WriteLine("Murf AI Error: WAV data chunk size exceeds available data.");
                    dataChunkSize = (int)(wavData.Length - reader.BaseStream.Position);
                    if (dataChunkSize < 0) dataChunkSize = 0;
                }

                byte[] pcmData = reader.ReadBytes(dataChunkSize);
                // --- End Parsing ---

                // Calculate duration if not already known from API response
                TimeSpan? duration = knownDuration;
                if (!duration.HasValue && sampleRate > 0 && bitsPerSample > 0 && channels > 0 && dataChunkSize > 0)
                {
                    double durationSeconds = (double)dataChunkSize / (sampleRate * channels * (bitsPerSample / 8));
                    duration = TimeSpan.FromSeconds(durationSeconds);
                }

                return (pcmData, duration);
            }
            catch (Exception ex)
            {
                // todo logging
                Console.WriteLine($"Murf AI WAV Parsing Error: {ex.Message}. Returning original WAV data.");
                return (wavData, knownDuration); // Return original if parsing fails
            }
        }


        public Task StopTextSynthesisAsync()
        {
            // Cancellation is handled via the CancellationToken passed to SynthesizeTextAsync
            return Task.CompletedTask;
        }

        public string GetProviderFullName()
        {
            return "MurfAITextToSpeech";
        }

        public InterfaceTTSProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public static InterfaceTTSProviderEnum GetProviderTypeStatic()
        {
            return InterfaceTTSProviderEnum.MurfAITextToSpeech;
        }

        public void Dispose()
        {
            // Static HttpClient doesn't need instance disposal
            GC.SuppressFinalize(this);
        }
    }
}