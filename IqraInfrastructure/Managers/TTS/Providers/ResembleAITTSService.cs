using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS.Providers.ResembleAI;
using IqraCore.Interfaces.AI;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class ResembleAITTSService : ITTSService, IDisposable
    {
        private static readonly HttpClient _httpClient = new();
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        private readonly string _apiKey;
        private readonly string _projectUuid;
        private readonly string _voiceUuid;
        
        private readonly int _targetSampleRate;
        private readonly string _precision = "PCM_16";

        private const string _streamingEndpointUrl = "https://f.cluster.resemble.ai/synthesize";

        public ResembleAITTSService(string apiKey, string projectUuid, string voiceUuid, int targetSampleRate)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentNullException(nameof(apiKey));
            if (string.IsNullOrWhiteSpace(projectUuid)) throw new ArgumentNullException(nameof(projectUuid));
            if (string.IsNullOrWhiteSpace(voiceUuid)) throw new ArgumentNullException(nameof(voiceUuid));
            if (targetSampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(targetSampleRate), "Target sample rate must be positive.");

            _apiKey = apiKey;
            _projectUuid = projectUuid;
            _voiceUuid = voiceUuid;
            _targetSampleRate = targetSampleRate;
        }

        public void Initialize()
        {
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
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "RIFF") return (Array.Empty<byte>(), TimeSpan.Zero, 0);
                reader.ReadInt32(); // Remaining file size
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "WAVE") return (Array.Empty<byte>(), TimeSpan.Zero, 0);

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
                        if (numChannels <= 0 || sampleRateFromWav <= 0 || bitsPerSample <= 0) return (Array.Empty<byte>(), TimeSpan.Zero, 0);

                        // Skip any extra fmt bytes
                        if (memoryStream.Position < nextChunkPos && nextChunkPos <= memoryStream.Length)
                            reader.BaseStream.Seek(nextChunkPos - memoryStream.Position, SeekOrigin.Current);
                        else if (nextChunkPos > memoryStream.Length) return (Array.Empty<byte>(), TimeSpan.Zero, 0); // overran
                    }
                    else if (chunkId.ToLowerInvariant() == "data")
                    {
                        if (!fmtFound) return (Array.Empty<byte>(), TimeSpan.Zero, 0); // Data before fmt
                        pcmData = reader.ReadBytes(chunkSize);
                        // Once data is found, we can break if we don't need other chunks
                        break;
                    }
                    else // Skip other chunks like "cue ", "list", "ltxt"
                    {
                        if (memoryStream.Position < nextChunkPos && nextChunkPos <= memoryStream.Length)
                            reader.BaseStream.Seek(nextChunkPos - memoryStream.Position, SeekOrigin.Current);
                        else if (nextChunkPos > memoryStream.Length) return (Array.Empty<byte>(), TimeSpan.Zero, 0); // overran
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


        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            int? requestSampleRate = null;
            int[] allowedResembleRates = { 8000, 16000, 22050, 32000, 44100, 48000 };
            if (Array.Exists(allowedResembleRates, r => r == _targetSampleRate))
            {
                requestSampleRate = _targetSampleRate;
            }

            var apiRequestPayload = new ResembleTtsApiRequest
            {
                ProjectUuid = _projectUuid,
                VoiceUuid = _voiceUuid,
                Data = text,
                Precision = _precision, // Always request PCM_16
                SampleRate = _targetSampleRate // Request our target SR. Resemble will use its default if this is not supported.
            };

            string jsonPayload = JsonSerializer.Serialize(apiRequestPayload, _jsonSerializerOptions);

            using var request = new HttpRequestMessage(HttpMethod.Post, _streamingEndpointUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            // Resemble doc says response is WAV, so we don't need specific Accept header for WAV, it's the default for this endpoint.

            try
            {
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    // string errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    // Console.WriteLine($"Resemble AI HTTP Error ({response.StatusCode}): {errorBody}");
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                byte[] wavData = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                if (wavData == null || wavData.Length == 0)
                {
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                var (pcmData, duration, originalSampleRate) = ParseWavAndExtractPcm(wavData);

                if (pcmData == null || pcmData.Length == 0 || originalSampleRate == 0)
                {
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                // Resemble returns 16-bit mono when PCM_16 is requested.
                byte[] finalPcmData = ResamplePcm(pcmData, originalSampleRate, _targetSampleRate, 1, 16);

                return (finalPcmData, duration);
            }
            catch (HttpRequestException) { return (Array.Empty<byte>(), TimeSpan.Zero); }
            catch (JsonException) { return (Array.Empty<byte>(), TimeSpan.Zero); }
            catch (TaskCanceledException) { return (Array.Empty<byte>(), TimeSpan.Zero); }
            catch (Exception) { return (Array.Empty<byte>(), TimeSpan.Zero); }
        }

        public Task StopTextSynthesisAsync()
        {
            return Task.CompletedTask;
        }

        public string GetProviderFullName()
        {
            return "ResembleAITextToSpeech";
        }

        public InterfaceTTSProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public static InterfaceTTSProviderEnum GetProviderTypeStatic()
        {
            return InterfaceTTSProviderEnum.ResembleAITextToSpeech;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
