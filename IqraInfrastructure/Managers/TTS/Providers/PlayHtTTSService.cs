using IqraCore.Entities.TTS.Providers.PlayHt;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class PlayHtTTSService : ITTSService, IDisposable
    {
        private static readonly HttpClient _httpClient = new();
        private readonly string _apiKey;
        private readonly string _userId;
        private readonly string _voiceId; // Can be ID, S3 URL, or specific name like "Atlas-PlayAI"
        private readonly string? _voiceEngine; // e.g., "PlayDialog", "Play3.0-mini"

        private readonly int _sampleRate = 16000; // Target sample rate
        private readonly int _characterLimit; // Varies by model

        private const string ApiUrl = "https://api.play.ht/api/v2/tts/stream";

        public PlayHtTTSService(string apiKey, string userId, string voiceId, string voiceEngine)
        {
            _apiKey = apiKey;
            _userId = userId;
            _voiceId = voiceId;
            _voiceEngine = voiceEngine;

            // Set character limit based on engine (defaulting to 2000 if engine is unknown/null)
            _characterLimit = _voiceEngine == "Play3.0-mini" ? 20000 : 2000;

            // Could add options for default sample rate, quality, speed etc.
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

            if (text.Length > _characterLimit)
            {
                // todo logging
                Console.WriteLine($"Play.ht TTS Error: Text exceeds the {_characterLimit} character limit for the '{_voiceEngine ?? "default"}' engine.");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }

            var requestPayload = new PlayHtTtsRequest
            {
                Text = text,
                Voice = _voiceId,
                VoiceEngine = _voiceEngine,
                OutputFormat = "wav", // Request WAV to get header metadata
                SampleRate = _sampleRate, // Request our target sample rate
                // Populate optional fields from metaData if needed
                // Speed = metaData?.TryGetValue("speed", out var speed) ? (double?)speed : null,
                // Emotion = metaData?.TryGetValue("emotion", out var emotion) ? (string?)emotion : null,
            };

            string jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            var requestUri = ApiUrl;

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            // Add authentication headers
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Headers.Add("X-USER-ID", _userId);
            // Set content type and accept headers
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/wav")); // We expect WAV

            try
            {
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    // Read the raw audio bytes directly from the response body
                    byte[] wavData = await response.Content.ReadAsByteArrayAsync(cancellationToken);

                    // Parse the WAV header to extract PCM data and calculate duration
                    // todo logging
                    Console.WriteLine($"Play.ht: Received WAV format. Parsing header...");
                    return ParseWavAndExtractPcm(wavData);
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    // todo logging
                    Console.WriteLine($"Play.ht API Error ({response.StatusCode}): {errorContent}");
                    // Consider parsing errorContent if it's structured JSON
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }
            }
            catch (HttpRequestException httpEx)
            {
                // todo logging
                Console.WriteLine($"Play.ht HTTP Request Error: {httpEx.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                // todo logging
                Console.WriteLine("Play.ht TTS synthesis was cancelled.");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                // todo logging
                Console.WriteLine($"Play.ht TTS Error: {ex.GetType().Name} - {ex.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        // WAV parser (reuse or adapt from FishAudio/HumeAI service)
        private (byte[]?, TimeSpan?) ParseWavAndExtractPcm(byte[] wavData)
        {
            try
            {
                if (wavData == null || wavData.Length < 44)
                {
                    // todo logging
                    Console.WriteLine("Play.ht Error: Received invalid or incomplete WAV data.");
                    return (null, null);
                }

                using var reader = new BinaryReader(new MemoryStream(wavData));

                // -- Basic WAV Header Parsing --
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "RIFF") return (wavData, null); // Return original if malformed
                reader.ReadInt32(); // File size - 8
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "WAVE") return (wavData, null);
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "fmt ") return (wavData, null);

                int fmtChunkSize = reader.ReadInt32();
                short audioFormat = reader.ReadInt16(); // 1 for PCM
                short channels = reader.ReadInt16();
                int sampleRate = reader.ReadInt32();
                reader.ReadInt32(); // Byte rate
                reader.ReadInt16(); // Block align
                short bitsPerSample = reader.ReadInt16();

                if (audioFormat != 1)
                {
                    // todo logging
                    Console.WriteLine("Play.ht Error: Received WAV is not in PCM format.");
                    return (wavData, null); // Return original data if not PCM
                }

                // Skip any extra fmt bytes
                if (fmtChunkSize > 16)
                    reader.BaseStream.Seek(fmtChunkSize - 16, SeekOrigin.Current);

                // Find 'data' chunk
                string dataChunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                // Handle potential LIST/INFO chunks before data chunk
                while (dataChunkId.ToLowerInvariant() != "data" && reader.BaseStream.Position < wavData.Length - 8)
                {
                    int listChunkSize = reader.ReadInt32();
                    if (listChunkSize <= 0 || reader.BaseStream.Position + listChunkSize > wavData.Length) // Basic sanity check
                        return (wavData, null); // Malformed chunk size
                    reader.BaseStream.Seek(listChunkSize, SeekOrigin.Current);
                    dataChunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                }
                if (dataChunkId.ToLowerInvariant() != "data")
                {
                    Console.WriteLine("Play.ht Error: 'data' chunk not found in WAV.");
                    return (wavData, null);
                }

                int dataChunkSize = reader.ReadInt32();
                if (reader.BaseStream.Position + dataChunkSize > wavData.Length)
                {
                    Console.WriteLine("Play.ht Error: WAV data chunk size exceeds available data.");
                    dataChunkSize = (int)(wavData.Length - reader.BaseStream.Position); // Read what's available
                    if (dataChunkSize < 0) dataChunkSize = 0;
                }

                byte[] pcmData = reader.ReadBytes(dataChunkSize);
                // --- End Parsing ---

                // Calculate duration
                TimeSpan? duration = null;
                if (sampleRate > 0 && bitsPerSample > 0 && channels > 0 && dataChunkSize > 0)
                {
                    double durationSeconds = (double)dataChunkSize / (sampleRate * channels * (bitsPerSample / 8));
                    duration = TimeSpan.FromSeconds(durationSeconds);
                }

                return (pcmData, duration);
            }
            catch (Exception ex)
            {
                // todo logging
                Console.WriteLine($"Play.ht WAV Parsing Error: {ex.Message}. Returning original WAV data.");
                // Return original data if parsing fails, duration unknown
                return (wavData, null);
            }
        }


        public Task StopTextSynthesisAsync()
        {
            // Cancellation is handled via the CancellationToken passed to SynthesizeTextAsync
            return Task.CompletedTask;
        }

        public string GetProviderFullName()
        {
            return "PlayHtTextToSpeech";
        }

        public InterfaceTTSProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public static InterfaceTTSProviderEnum GetProviderTypeStatic()
        {
            return InterfaceTTSProviderEnum.PlayHtTextToSpeech;
        }

        public void Dispose()
        {
            // Static HttpClient doesn't need instance disposal
            GC.SuppressFinalize(this);
        }
    }
}