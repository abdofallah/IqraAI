using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS.Providers.FishAudio;
using IqraCore.Interfaces.AI;
using MessagePack;
using System.Net.Http.Headers;
using System.Text;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class FishAudioTTSService : ITTSService, IDisposable
    {
        private static readonly HttpClient _httpClient = new();
        private readonly string _apiKey;
        private readonly string _referenceId;
        private readonly string _model;

        private const string ApiUrl = "https://api.fish.audio/v1/tts";

        public FishAudioTTSService(string apiKey, string referenceId, string model)
        {
            _apiKey = apiKey;
            _referenceId = referenceId;
            _model = model;
        }

        public void Initialize()
        {
            // Static HttpClient, initialization done in constructor or is implicit
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            if (string.IsNullOrEmpty(text))
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }

            var requestPayload = new FishAudioTTSRequest
            {
                Text = text,
                ReferenceId = _referenceId,
                Format = "wav"
            };

            byte[] messagePackData;
            try
            {
                messagePackData = MessagePackSerializer.Serialize(requestPayload);
            }
            catch (Exception ex)
            {
                // todo logging
                Console.WriteLine($"Fish Audio MessagePack Serialization Error: {ex.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }


            using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/wav"));
            request.Headers.Add("model", _model);

            request.Content = new ByteArrayContent(messagePackData);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/msgpack");

            try
            {
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    byte[] wavData = await response.Content.ReadAsByteArrayAsync(cancellationToken);

                    // Parse WAV header to extract PCM data and calculate duration
                    return ParseWavHeaderAndExtractPcm(wavData);
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync(cancellationToken); // Might not be string if error is msgpack
                    //Console.WriteLine($"Fish Audio API Error ({response.StatusCode}): {errorContent}");
                    // todo logging
                    // Consider trying to deserialize errorContent if it's msgpack
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }
            }
            catch (HttpRequestException ex)
            {
                // todo logging
                // Console.WriteLine($"Fish Audio HTTP Request Error: {ex.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                // todo logging
                // Console.WriteLine("Fish Audio TTS synthesis was cancelled.");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (Exception ex) // Catch broader exceptions, including MessagePack errors on response
            {
                // todo logging
                // Console.WriteLine($"Fish Audio TTS Error: {ex.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        private (byte[]?, TimeSpan?) ParseWavHeaderAndExtractPcm(byte[] wavData)
        {
            try
            {
                if (wavData == null || wavData.Length < 44) // Basic check for header size
                {
                    Console.WriteLine("Fish Audio Error: Received invalid or incomplete WAV data.");
                    return (null, null);
                }

                using var reader = new BinaryReader(new MemoryStream(wavData));

                // Read and validate chunks (simple validation)
                string riff = Encoding.ASCII.GetString(reader.ReadBytes(4));
                if (riff != "RIFF") return (null, null);
                reader.ReadInt32(); // File size - 8
                string wave = Encoding.ASCII.GetString(reader.ReadBytes(4));
                if (wave != "WAVE") return (null, null);
                string fmtChunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                if (fmtChunkId != "fmt ") return (null, null);

                int fmtChunkSize = reader.ReadInt32();
                reader.ReadInt16(); // Audio format (1 for PCM)
                short channels = reader.ReadInt16();
                int sampleRate = reader.ReadInt32();
                reader.ReadInt32(); // Byte rate
                reader.ReadInt16(); // Block align
                short bitsPerSample = reader.ReadInt16();

                // Skip any extra fmt bytes
                reader.BaseStream.Seek(fmtChunkSize - 16, SeekOrigin.Current);

                // Find 'data' chunk
                string dataChunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                // Handle potential LIST chunk before data chunk
                while (dataChunkId != "data" && reader.BaseStream.Position < wavData.Length - 8)
                {
                    int listChunkSize = reader.ReadInt32();
                    reader.BaseStream.Seek(listChunkSize, SeekOrigin.Current);
                    dataChunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                }
                if (dataChunkId != "data") return (null, null);


                int dataChunkSize = reader.ReadInt32();

                // Read the actual audio data (PCM)
                byte[] pcmData = reader.ReadBytes(dataChunkSize);

                if (sampleRate <= 0 || bitsPerSample <= 0 || channels <= 0 || dataChunkSize <= 0)
                {
                    Console.WriteLine("Fish Audio Error: Invalid WAV header parameters.");
                    return (pcmData, null); // Return data even if duration calculation fails
                }

                // Calculate duration
                double durationSeconds = (double)dataChunkSize / (sampleRate * channels * (bitsPerSample / 8));
                TimeSpan duration = TimeSpan.FromSeconds(durationSeconds);

                // Return *only* the raw PCM data and calculated duration
                return (pcmData, duration);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fish Audio WAV Parsing Error: {ex.Message}");
                // Return original data if parsing fails, but duration is unknown
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
            return "FishAudioTextToSpeech";
        }

        public InterfaceTTSProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public static InterfaceTTSProviderEnum GetProviderTypeStatic()
        {
            return InterfaceTTSProviderEnum.FishAudioTextToSpeech;
        }

        public void Dispose()
        {
            // Static HttpClient doesn't need instance disposal
            GC.SuppressFinalize(this);
        }
    }
}