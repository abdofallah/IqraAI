using IqraCore.Entities.TTS.Providers.PlayHt;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Protobuf.Reflection;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class PlayHtTTSService : ITTSService, IDisposable
    {
        private static readonly HttpClient _httpClient = new();
        private readonly string _apiKey;
        private readonly string _userId;
        private readonly string _voiceId;
        private readonly string _voiceEngine;
        private readonly string _voiceQuality;
        private readonly float _voiceSpeed;
        private readonly float _temperature;
        private readonly string _emotion;
        private readonly float _voiceGuidance;
        private readonly float _styleGuidance;
        private readonly float _textGuidance;
        private readonly string _language;

        private readonly int _sampleRate;
        private string _audioFormat;

        private const string ApiUrl = "https://api.play.ht/api/v2/tts/stream";

        public PlayHtTTSService(string apiKey, string userId, string voiceId, string voiceEngine, string voiceQuality, float voiceSpeed, float temperature, string emotion, float voiceGuidance, float styleGuidance, float textGuidance, string language, int sampleRate)
        {
            _apiKey = apiKey;
            _userId = userId;
            _voiceId = voiceId;
            _voiceEngine = voiceEngine;
            _voiceQuality = voiceQuality;
            _voiceSpeed = voiceSpeed;
            _temperature = temperature;
            _emotion = emotion;
            _voiceGuidance = voiceGuidance;
            _styleGuidance = styleGuidance;
            _textGuidance = textGuidance;
            _language = language;
            _sampleRate = sampleRate;
        }

        public void Initialize()
        {
            // make this dynamic within dashboard
            if (_voiceEngine == "PlayHT1.0")
            {
                if (_sampleRate != 8000)
                {
                    throw new Exception("Unsupported sample rate for PlayHT1.0, supported are: 8000");
                }
                _audioFormat = "mulaw"; // todo maybe make it mp3 instead for higher quality unless its also 8000

            }
            else
            {
                _audioFormat = "wav";
            }

            if (_sampleRate < 8000 || _sampleRate > 48000)
            {
                throw new Exception("Unsupported sample rate, supported are: 8000~48000");
            }
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            if (string.IsNullOrEmpty(text))
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }

            var requestPayload = new PlayHtTtsRequest
            {
                Text = text,
                Voice = _voiceId,
                VoiceEngine = _voiceEngine,
                Quality = _voiceQuality,
                OutputFormat = _audioFormat,
                SampleRate = _sampleRate,
                Speed = _voiceSpeed,
                Temperature = _temperature,
                Emotion = _emotion,
                VoiceGuidance = _voiceGuidance,
                StyleGuidance = _styleGuidance,
                TextGuidance = _textGuidance,
                Language = _language
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

                    // todo confirm this is wav format not mulaw or mp3
                    // Parse the WAV header to extract PCM data and calculate duration
                    // todo logging
                    Console.WriteLine($"Play.ht: Received WAV format. Parsing header...");
                    return ParseAudioAndExtractPcm(wavData);
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

        private (byte[]?, TimeSpan?) ParseAudioAndExtractPcm(byte[] audioData)
        {
            try
            {
                if (_audioFormat == "ULAW")
                {
                    double durationSeconds = (double)audioData.Length / _sampleRate;
                    var ulawDuration = TimeSpan.FromSeconds(durationSeconds);

                    return (audioData, ulawDuration);
                }

                if (audioData == null || audioData.Length < 44)
                {
                    // todo logging
                    Console.WriteLine("Play.ht Error: Received invalid or incomplete WAV data.");
                    return (null, null);
                }

                using var reader = new BinaryReader(new MemoryStream(audioData));

                // -- Basic WAV Header Parsing --
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "RIFF") return (audioData, null); // Return original if malformed
                reader.ReadInt32(); // File size - 8
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "WAVE") return (audioData, null);
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "fmt ") return (audioData, null);

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
                    return (audioData, null); // Return original data if not PCM
                }

                // Skip any extra fmt bytes
                if (fmtChunkSize > 16)
                    reader.BaseStream.Seek(fmtChunkSize - 16, SeekOrigin.Current);

                // Find 'data' chunk
                string dataChunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                // Handle potential LIST/INFO chunks before data chunk
                while (dataChunkId.ToLowerInvariant() != "data" && reader.BaseStream.Position < audioData.Length - 8)
                {
                    int listChunkSize = reader.ReadInt32();
                    if (listChunkSize <= 0 || reader.BaseStream.Position + listChunkSize > audioData.Length) // Basic sanity check
                        return (audioData, null); // Malformed chunk size
                    reader.BaseStream.Seek(listChunkSize, SeekOrigin.Current);
                    dataChunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                }
                if (dataChunkId.ToLowerInvariant() != "data")
                {
                    Console.WriteLine("Play.ht Error: 'data' chunk not found in WAV.");
                    return (audioData, null);
                }

                int dataChunkSize = reader.ReadInt32();
                if (reader.BaseStream.Position + dataChunkSize > audioData.Length)
                {
                    Console.WriteLine("Play.ht Error: WAV data chunk size exceeds available data.");
                    dataChunkSize = (int)(audioData.Length - reader.BaseStream.Position); // Read what's available
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
                return (audioData, null);
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
            GC.SuppressFinalize(this);
        }
    }
}