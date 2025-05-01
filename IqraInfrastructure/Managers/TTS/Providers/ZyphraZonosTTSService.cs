using IqraCore.Entities.TTS.Providers.ZyphraZonos;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class ZyphraZonosTTSService : ITTSService, IDisposable
    {
        private static readonly HttpClient _httpClient = new();
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string? _defaultVoiceName;

        private const string ApiUrl = "https://api.zyphra.com/v1/audio/text-to-speech";

        // pcm s16 le 48khz 16-bit mono
        private readonly int _sampleRate = 48000; // can not be changed yet default by api
        private readonly int _sampleSize = 16; // can not be changed default by api
        private readonly int _channels = 1; // can not be changed default by api

        // Constructor
        public ZyphraZonosTTSService(string apiKey, string model, string? defaultVoiceName)
        {
            _apiKey = apiKey;
            _model = model;
            _defaultVoiceName = defaultVoiceName; // Can be null if no default voice desired
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
            // Note: API limits are not explicitly stated for text length in docs.

            var requestPayload = new ZyphraTtsRequest
            {
                Text = text,
                Model = _model,
                MimeType = "audio/wav", // Request WAV
                DefaultVoiceName = _defaultVoiceName // Use the default voice set in constructor
                // Populate optional fields from metaData if needed
                // SpeakingRate = metaData?.TryGetValue("speaking_rate", out var rate) ? (double?)rate : null,
                // LanguageIsoCode = metaData?.TryGetValue("language", out var lang) ? (string?)lang : null,
                // VoiceName = metaData?.TryGetValue("custom_voice", out var custom) ? (string?)custom : null, // Allow overriding default
            };

            // Allow overriding default voice or setting custom voice via metaData
            if (metaData?.TryGetValue("voice_name", out var voiceNameObj) == true && voiceNameObj is string voiceName && !string.IsNullOrWhiteSpace(voiceName))
            {
                requestPayload.VoiceName = voiceName;
                requestPayload.DefaultVoiceName = null; // Ensure only one voice type is set
            }
            else if (metaData?.TryGetValue("default_voice_name", out var defaultVoiceObj) == true && defaultVoiceObj is string defaultVoice && !string.IsNullOrWhiteSpace(defaultVoice))
            {
                requestPayload.DefaultVoiceName = defaultVoice;
                requestPayload.VoiceName = null; // Ensure only one voice type is set
            }

            // Add other optional params from metaData
            if (metaData?.TryGetValue("speaking_rate", out var rateObj) == true && (rateObj is double || rateObj is int || rateObj is float))
            {
                requestPayload.SpeakingRate = Convert.ToDouble(rateObj);
            }
            if (metaData?.TryGetValue("language", out var langObj) == true && langObj is string langCode)
            {
                requestPayload.LanguageIsoCode = langCode;
            }
            // Add logic for emotion, pitchStd, vqscore, speaker_noised based on metaData if desired


            string jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            var requestUri = ApiUrl;

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Add("X-API-Key", _apiKey); // Use X-API-Key header
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
                    return ParseWavAndExtractPcm(wavData);
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    // todo logging
                    Console.WriteLine($"Zyphra Zonos API Error ({response.StatusCode}): {errorContent}");
                    // Consider parsing errorContent if it's structured JSON
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }
            }
            catch (HttpRequestException httpEx)
            {
                // todo logging
                Console.WriteLine($"Zyphra Zonos HTTP Request Error: {httpEx.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                // todo logging
                Console.WriteLine("Zyphra Zonos TTS synthesis was cancelled.");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                // todo logging
                Console.WriteLine($"Zyphra Zonos TTS Error: {ex.GetType().Name} - {ex.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        // WAV parser (reuse or adapt from previous implementations)
        private (byte[]?, TimeSpan?) ParseWavAndExtractPcm(byte[] wavData)
        {
            try
            {
                if (wavData == null || wavData.Length < 44)
                {
                    // todo logging
                    Console.WriteLine("Zyphra Zonos Error: Received invalid or incomplete WAV data.");
                    return (null, null);
                }

                using var reader = new BinaryReader(new MemoryStream(wavData));
                // --- Basic WAV Header Parsing ---
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "RIFF") return (wavData, null);
                reader.ReadInt32(); // File size - 8
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "WAVE") return (wavData, null);
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "fmt ") return (wavData, null);

                int fmtChunkSize = reader.ReadInt32();
                short audioFormat = reader.ReadInt16();
                short channels = reader.ReadInt16();
                int sampleRate = reader.ReadInt32();
                reader.ReadInt32(); // Byte rate
                reader.ReadInt16(); // Block align
                short bitsPerSample = reader.ReadInt16();

                if (audioFormat != 1)
                {
                    Console.WriteLine("Zyphra Zonos Error: Received WAV is not in PCM format.");
                    return (wavData, null);
                }
                if (fmtChunkSize > 16)
                    reader.BaseStream.Seek(fmtChunkSize - 16, SeekOrigin.Current);

                string dataChunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                while (dataChunkId.ToLowerInvariant() != "data" && reader.BaseStream.Position < wavData.Length - 8)
                {
                    int listChunkSize = reader.ReadInt32();
                    if (listChunkSize <= 0 || reader.BaseStream.Position + listChunkSize > wavData.Length)
                        return (wavData, null);
                    reader.BaseStream.Seek(listChunkSize, SeekOrigin.Current);
                    dataChunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                }
                if (dataChunkId.ToLowerInvariant() != "data")
                {
                    Console.WriteLine("Zyphra Zonos Error: 'data' chunk not found in WAV.");
                    return (wavData, null);
                }

                int dataChunkSize = reader.ReadInt32();
                if (reader.BaseStream.Position + dataChunkSize > wavData.Length)
                {
                    Console.WriteLine("Zyphra Zonos Error: WAV data chunk size exceeds available data.");
                    dataChunkSize = (int)(wavData.Length - reader.BaseStream.Position);
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
                Console.WriteLine($"Zyphra Zonos WAV Parsing Error: {ex.Message}. Returning original WAV data.");
                return (wavData, null); // Return original if parsing fails
            }
        }

        public Task StopTextSynthesisAsync()
        {
            // Cancellation is handled via the CancellationToken passed to SynthesizeTextAsync
            return Task.CompletedTask;
        }

        public string GetProviderFullName()
        {
            return "ZyphraZonosTextToSpeech";
        }

        public InterfaceTTSProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public static InterfaceTTSProviderEnum GetProviderTypeStatic()
        {
            return InterfaceTTSProviderEnum.ZyphraZonosTextToSpeech;
        }

        public void Dispose()
        {
            // Static HttpClient doesn't need instance disposal
            GC.SuppressFinalize(this);
        }
    }
}