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
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/wav"));

            try
            {
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                byte[] wavData = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                return ParseWavAndResample(wavData);
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
            if (pcmData.Length % 2 != 0) return Array.Empty<short>(); // Invalid for 16-bit
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
                return pcmData;
            }

            short[] inputShorts = PcmBytesToShorts(pcmData);
            if (inputShorts.Length == 0 && pcmData.Length > 0) return pcmData; // Conversion failed somehow

            int inputFrames = inputShorts.Length / numChannels;
            if (inputFrames == 0 && inputShorts.Length > 0) return pcmData; // Not enough samples for even one frame


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
                    if (inputIndex1 < 0 && inputShorts.Length > 0) inputIndex1 = 0; // Final safeguard if length is very small


                    short sample1 = (inputShorts.Length > 0 && inputIndex1 < inputShorts.Length) ? inputShorts[inputIndex1] : (short)0;

                    if (originalSampleRate < targetSampleRate)
                    {
                        int inputIndex2 = ((baseInputFrameFloor + 1) * numChannels) + c;
                        if (inputIndex2 >= inputShorts.Length) inputIndex2 = inputIndex1;

                        short sample2 = (inputShorts.Length > 0 && inputIndex2 < inputShorts.Length) ? inputShorts[inputIndex2] : sample1; // Use sample1 if sample2 invalid
                        double fraction = originalFrameIndexDouble - baseInputFrameFloor;
                        outputShorts[i * numChannels + c] = (short)(sample1 * (1.0 - fraction) + sample2 * fraction);
                    }
                    else
                    {
                        outputShorts[i * numChannels + c] = sample1;
                    }
                }
            }
            return PcmShortsToBytes(outputShorts);
        }

        private (byte[]?, TimeSpan?) ParseWavAndResample(byte[] wavData)
        {
            if (wavData == null || wavData.Length < 44)
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }

            using var memoryStream = new MemoryStream(wavData);
            using var reader = new BinaryReader(memoryStream);

            try
            {
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "RIFF") return (Array.Empty<byte>(), TimeSpan.Zero);
                reader.ReadInt32();
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "WAVE") return (Array.Empty<byte>(), TimeSpan.Zero);

                string chunkId = string.Empty;
                int chunkSize;

                short numChannelsFromWav = 0;
                int sampleRateFromWav = 0;
                int byteRateFromWav = 0;
                short bitsPerSampleFromWav = 0;
                bool fmtFound = false;

                while (memoryStream.Position + 8 <= memoryStream.Length) // Need at least 8 bytes for chunk ID and size
                {
                    chunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                    chunkSize = reader.ReadInt32();

                    if (memoryStream.Position + chunkSize > memoryStream.Length || chunkSize < 0) return (Array.Empty<byte>(), TimeSpan.Zero);

                    if (chunkId.ToLowerInvariant() == "fmt ")
                    {
                        if (chunkSize < 16) return (Array.Empty<byte>(), TimeSpan.Zero);

                        long fmtChunkEndPos = memoryStream.Position + chunkSize;

                        reader.ReadInt16(); // audioFormat (assume PCM=1)
                        numChannelsFromWav = reader.ReadInt16();
                        sampleRateFromWav = reader.ReadInt32();
                        byteRateFromWav = reader.ReadInt32();
                        reader.ReadInt16(); // blockAlign
                        bitsPerSampleFromWav = reader.ReadInt16();
                        fmtFound = true;

                        if (numChannelsFromWav <= 0 || sampleRateFromWav <= 0 || bitsPerSampleFromWav <= 0)
                            return (Array.Empty<byte>(), TimeSpan.Zero); // Invalid fmt params

                        if (memoryStream.Position < fmtChunkEndPos)
                            reader.BaseStream.Seek(fmtChunkEndPos - memoryStream.Position, SeekOrigin.Current); // Skip rest of fmt chunk
                        else if (memoryStream.Position > fmtChunkEndPos) return (Array.Empty<byte>(), TimeSpan.Zero); // Overran
                    }
                    else if (chunkId.ToLowerInvariant() == "data")
                    {
                        if (!fmtFound) return (Array.Empty<byte>(), TimeSpan.Zero); // Data chunk before fmt

                        byte[] pcmData = reader.ReadBytes(chunkSize);
                        TimeSpan duration = TimeSpan.Zero;
                        if (byteRateFromWav > 0 && chunkSize > 0)
                        {
                            duration = TimeSpan.FromSeconds((double)chunkSize / byteRateFromWav);
                        }
                        else if (sampleRateFromWav > 0 && numChannelsFromWav > 0 && bitsPerSampleFromWav > 0 && chunkSize > 0)
                        {
                            double bytesPerSampleCalc = bitsPerSampleFromWav / 8.0;
                            double totalFrames = chunkSize / (bytesPerSampleCalc * numChannelsFromWav);
                            duration = TimeSpan.FromSeconds(totalFrames / sampleRateFromWav);
                        }

                        byte[] finalPcmData = ResamplePcm(pcmData, sampleRateFromWav, _targetSampleRate, numChannelsFromWav, bitsPerSampleFromWav);
                        return (finalPcmData, duration);
                    }
                    else // Skip unknown chunks
                    {
                        reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                    }
                }
                return (Array.Empty<byte>(), TimeSpan.Zero); // Data chunk not found or fmt not found
            }
            catch (EndOfStreamException)
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (ArgumentOutOfRangeException)
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (IOException)
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (Exception)
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
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
