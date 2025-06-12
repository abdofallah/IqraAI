using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using IqraCore.Entities.TTS.Providers.Hamsa;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class HamsaAITTSService : ITTSService, IDisposable
    {
        private static readonly HttpClient _httpClient = new();
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

        private readonly string _apiKey;
        private readonly string _speaker;
        private readonly string? _dialect;
        private readonly int _targetSampleRate;

        private const string ApiUrl = "https://api.tryhamsa.com/v1/realtime/tts";

        public HamsaAITTSService(string apiKey, string speaker, string dialect, int targetSampleRate = 8000)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentNullException(nameof(apiKey));
            if (string.IsNullOrWhiteSpace(speaker)) throw new ArgumentNullException(nameof(speaker));
            if (string.IsNullOrWhiteSpace(dialect)) throw new ArgumentNullException(nameof(dialect));
            if (targetSampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(targetSampleRate), "Target sample rate must be positive.");

            _apiKey = apiKey;
            _speaker = speaker;
            _dialect = dialect;
            _targetSampleRate = targetSampleRate;
        }

        public void Initialize()
        {
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            var requestPayload = new HamsaTtsApiRequest
            {
                Text = text,
                Speaker = _speaker,
                Dialect = _dialect
            };

            string jsonPayload = JsonSerializer.Serialize(requestPayload, _jsonSerializerOptions);

            using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Token", _apiKey);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                byte[] wavData = await response.Content.ReadAsByteArrayAsync(cancellationToken);

                byte[]? pcmData = null;
                int originalChannels = 1; // Assume mono for PCM or WAV
                int originalBitsPerSample = 16; // Assume 16-bit for PCM or WAV as requested due to _precision being pcm_16

                var wavParseResult = ParseWavAndExtractPcm(wavData);
                pcmData = wavParseResult.pcmData;

                byte[] finalPcmData = ResamplePcm(pcmData, wavParseResult.originalSampleRate, _targetSampleRate, originalChannels, originalBitsPerSample);

                return (finalPcmData, wavParseResult.duration ?? TimeSpan.Zero);
            }
            catch (HttpRequestException)
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (JsonException)
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (Exception)
            {
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
                // This shouldn't happen if Resemble always returns 16-bit mono when requested and WAV parser is correct
                return pcmData;
            }

            short[] inputShorts = PcmBytesToShorts(pcmData);
            if (inputShorts.Length == 0 && pcmData.Length > 0) return pcmData;

            int inputFrames = inputShorts.Length / numChannels;
            if (inputFrames == 0 && inputShorts.Length > 0) return pcmData;


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
                    else // Downsampling
                    {
                        outputShorts[i * numChannels + c] = sample1;
                    }
                }
            }
            return PcmShortsToBytes(outputShorts);
        }

        private (byte[]? pcmData, TimeSpan? duration, int originalSampleRate) ParseWavAndExtractPcm(byte[] wavData)
        {
            if (wavData == null || wavData.Length < 44)
            {
                return (Array.Empty<byte>(), TimeSpan.Zero, 0);
            }

            using var memoryStream = new MemoryStream(wavData);
            using var reader = new BinaryReader(memoryStream);

            try
            {
                // RIFF chunk
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "RIFF")
                    return (Array.Empty<byte>(), TimeSpan.Zero, 0);
                reader.ReadInt32(); // Remaining file size
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "WAVE")
                    return (Array.Empty<byte>(), TimeSpan.Zero, 0);

                // Find 'fmt ' and 'data' chunks
                string chunkId;
                int chunkSize;
                short numChannels = 0;
                int sampleRateFromWav = 0;
                int byteRate = 0;
                short bitsPerSample = 0;
                bool fmtFound = false;
                byte[]? pcmData = null;

                while (memoryStream.Position + 8 <= memoryStream.Length) // Min 8 bytes for ID and size
                {
                    chunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                    chunkSize = reader.ReadInt32();

                    // temp fix for data broken chunk size
                    if (chunkId == "data" && chunkSize == -1)
                    {
                        chunkSize = (int)(memoryStream.Length - memoryStream.Position);
                    }

                    if (memoryStream.Position + chunkSize > memoryStream.Length || chunkSize < 0)
                        return (Array.Empty<byte>(), TimeSpan.Zero, 0); // Invalid chunk size

                    long nextChunkPos = memoryStream.Position + chunkSize;
                    // Align to 2-byte boundary if chunk size is odd (as per Resemble's ltxt note, applies generally to RIFF chunks)
                    if (chunkSize % 2 != 0) nextChunkPos++;


                    if (chunkId.ToLowerInvariant() == "fmt ")
                    {
                        if (chunkSize < 16) return (Array.Empty<byte>(), TimeSpan.Zero, 0);
                        reader.ReadInt16(); // Compression code (PCM = 1)
                        numChannels = reader.ReadInt16();
                        sampleRateFromWav = reader.ReadInt32();
                        byteRate = reader.ReadInt32();
                        reader.ReadInt16(); // Block align
                        bitsPerSample = reader.ReadInt16();
                        fmtFound = true;
                        if (numChannels <= 0 || sampleRateFromWav <= 0 || bitsPerSample <= 0) 
                            return (Array.Empty<byte>(), TimeSpan.Zero, 0);

                        // Skip any extra fmt bytes
                        if (memoryStream.Position < nextChunkPos && nextChunkPos <= memoryStream.Length)
                            reader.BaseStream.Seek(nextChunkPos - memoryStream.Position, SeekOrigin.Current);
                        else if (nextChunkPos > memoryStream.Length)
                            return (Array.Empty<byte>(), TimeSpan.Zero, 0); // overran
                    }
                    else if (chunkId.ToLowerInvariant() == "data")
                    {
                        if (!fmtFound)
                            return (Array.Empty<byte>(), TimeSpan.Zero, 0); // Data before fmt
                        pcmData = reader.ReadBytes(chunkSize);
                        // Once data is found, we can break if we don't need other chunks
                        break;
                    }
                    else // Skip other chunks like "cue ", "list", "ltxt"
                    {
                        if (memoryStream.Position < nextChunkPos && nextChunkPos <= memoryStream.Length)
                            reader.BaseStream.Seek(nextChunkPos - memoryStream.Position, SeekOrigin.Current);
                        else if (nextChunkPos > memoryStream.Length)
                            return (Array.Empty<byte>(), TimeSpan.Zero, 0); // overran
                    }
                }

                if (pcmData == null || pcmData.Length == 0 || !fmtFound)
                {
                    return (Array.Empty<byte>(), TimeSpan.Zero, 0);
                }

                TimeSpan duration = TimeSpan.Zero;
                if (byteRate > 0 && pcmData.Length > 0)
                {
                    duration = TimeSpan.FromSeconds((double)pcmData.Length / byteRate);
                }
                else if (sampleRateFromWav > 0 && numChannels > 0 && bitsPerSample > 0 && pcmData.Length > 0) // Fallback duration calculation
                {
                    double bytesPerSampleCalc = bitsPerSample / 8.0;
                    if (bytesPerSampleCalc == 0) return (pcmData, TimeSpan.Zero, sampleRateFromWav);
                    double totalFrames = pcmData.Length / (bytesPerSampleCalc * numChannels);
                    duration = TimeSpan.FromSeconds(totalFrames / sampleRateFromWav);
                }


                return (pcmData, duration, sampleRateFromWav);
            }
            catch (EndOfStreamException) { return (Array.Empty<byte>(), TimeSpan.Zero, 0); }
            catch (IOException) { return (Array.Empty<byte>(), TimeSpan.Zero, 0); }
            catch (Exception) { return (Array.Empty<byte>(), TimeSpan.Zero, 0); }
        }

        public Task StopTextSynthesisAsync()
        {
            return Task.CompletedTask;
        }

        public string GetProviderFullName()
        {
            return "HamsaAITextToSpeech";
        }

        public InterfaceTTSProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public static InterfaceTTSProviderEnum GetProviderTypeStatic()
        {
            return InterfaceTTSProviderEnum.HamsaAITextToSpeech;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
