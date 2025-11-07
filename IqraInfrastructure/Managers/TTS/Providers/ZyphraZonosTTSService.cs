using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS.Providers.ZyphraZonos;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.TTS;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class ZyphraZonosTTSService : ITTSService, IDisposable
    {
        private readonly string _apiKey;
        private readonly ZyphraZonosConfig _serviceConfig;

        private const string ApiUrl = "http://api.zyphra.com/v1/audio/text-to-speech";

        // Hardcoded defaults as per API documentation
        private readonly int _sampleSize = 16;
        private readonly int _channels = 1;

        private static readonly HttpClient _httpClient = new();

        // Constructor
        public ZyphraZonosTTSService(string apiKey, ZyphraZonosConfig config)
        {
            _apiKey = apiKey;
            _serviceConfig = config;
        }

        public async Task<FunctionReturnResult> Initialize()
        {
            var result = new FunctionReturnResult();

            return result.SetSuccessResult();
            // Static HttpClient initialization is handled implicitly
        }

        public async Task<FunctionReturnResult> CheckAccount()
        {
            var result = new FunctionReturnResult();

            try
            {
                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    $"CheckAccount:EXCEPTION",
                    $"Internal server error occured: {ex.Message}"
                );
            }
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
                Model = _serviceConfig.Model,
                MimeType = "audio/wav",
                DefaultVoiceName = _serviceConfig.DefaultVoiceName,
                SpeakingRate = _serviceConfig.SpeakingRate,
                LanguageIsoCode = _serviceConfig.LanguageIsoCode,
                Vqscore = _serviceConfig.Vqscore,
                Emotion = _serviceConfig.Emotion
            };

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

                    // Parse WAV to get PCM and actual audio parameters
                    var (pcmData, originalSampleRate, originalChannels, originalBitsPerSample, calculatedDuration) = ParseWavAndExtractPcmDetails(wavData);

                    if (pcmData == null || pcmData.Length == 0 || originalSampleRate == 0)
                    {
                        Console.WriteLine("Speechify Error: Failed to parse WAV data or extract PCM.");
                        return (Array.Empty<byte>(), TimeSpan.Zero);
                    }

                    // Use duration from API if available, otherwise use calculated duration
                    TimeSpan finalDuration = calculatedDuration ?? TimeSpan.Zero;

                    // Resample the PCM data
                    byte[] finalPcmData = ResamplePcm(pcmData, originalSampleRate, _serviceConfig.TargetSampleRate, originalChannels, _serviceConfig.TargetBitsPerSample);

                    return (finalPcmData, finalDuration);
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

        private static short[] PcmBytesToShorts(byte[] pcmData, int bitsPerSample)
        {
            if (bitsPerSample != 16) return Array.Empty<short>(); // Only support 16-bit for now
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

            // We primarily support resampling 16-bit PCM.
            if (bitsPerSample != 16)
            {
                // todo logging
                Console.WriteLine($"Speechify Resample: Unsupported bits per sample ({bitsPerSample}). Only 16-bit is supported for resampling. Returning original data.");
                return pcmData;
            }
            if (numChannels <= 0 || originalSampleRate <= 0)
            {
                // todo logging
                Console.WriteLine($"Speechify Resample: Invalid audio parameters (Channels: {numChannels}, OriginalRate: {originalSampleRate}). Returning original data.");
                return pcmData;
            }


            short[] inputShorts = PcmBytesToShorts(pcmData, bitsPerSample);
            if (inputShorts.Length == 0 && pcmData.Length > 0)
            {
                // todo logging
                Console.WriteLine("Speechify Resample: Failed to convert PCM bytes to shorts (possibly due to non-16-bit audio or odd length). Returning original data.");
                return pcmData;
            }


            int inputFrames = inputShorts.Length / numChannels;
            if (inputFrames == 0 && inputShorts.Length > 0)
            {
                // todo logging
                Console.WriteLine("Speechify Resample: Not enough samples for even one frame. Returning original data.");
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
                    else // Downsampling
                    {
                        outputShorts[i * numChannels + c] = sample1;
                    }
                }
            }
            return PcmShortsToBytes(outputShorts);
        }

        private (byte[]? pcmData, int sampleRate, int channels, int bitsPerSample, TimeSpan? duration) ParseWavAndExtractPcmDetails(byte[] wavData)
        {
            if (wavData == null || wavData.Length < 44) return (null, 0, 0, 0, null);

            using var memoryStream = new MemoryStream(wavData);
            using var reader = new BinaryReader(memoryStream);

            try
            {
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "RIFF") return (null, 0, 0, 0, null);
                reader.ReadInt32(); // File size - 8
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "WAVE") return (null, 0, 0, 0, null);

                string chunkId;
                int chunkSize;
                short numChannels = 0;
                int sampleRateFromWav = 0;
                short bitsPerSampleFromWav = 0;
                int byteRate = 0;
                bool fmtFound = false;
                byte[]? pcmBuffer = null;

                while (memoryStream.Position + 8 <= memoryStream.Length)
                {
                    chunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                    chunkSize = reader.ReadInt32();

                    // temp wav chunk size fix
                    if (chunkId == "data" && chunkSize == -1)
                    {
                        chunkSize = (int)(memoryStream.Length - memoryStream.Position);
                    }

                    if (memoryStream.Position + chunkSize > memoryStream.Length || chunkSize < 0)
                        return (null, 0, 0, 0, null);

                    long nextChunkPos = memoryStream.Position + chunkSize;
                    if (chunkSize % 2 != 0) nextChunkPos++; // RIFF chunk alignment

                    if (chunkId.ToLowerInvariant() == "fmt ")
                    {
                        if (chunkSize < 16) return (null, 0, 0, 0, null);
                        short audioFormat = reader.ReadInt16();
                        if (audioFormat != 1) { Console.WriteLine("Speechify WAV Error: Not PCM format."); return (null, 0, 0, 0, null); } // PCM = 1

                        numChannels = reader.ReadInt16();
                        sampleRateFromWav = reader.ReadInt32();
                        byteRate = reader.ReadInt32();
                        reader.ReadInt16(); // Block align
                        bitsPerSampleFromWav = reader.ReadInt16();
                        fmtFound = true;

                        if (numChannels <= 0 || sampleRateFromWav <= 0 || bitsPerSampleFromWav <= 0) return (null, 0, 0, 0, null);

                        if (memoryStream.Position < nextChunkPos && nextChunkPos <= memoryStream.Length)
                            reader.BaseStream.Seek(nextChunkPos - memoryStream.Position, SeekOrigin.Current);
                        else if (nextChunkPos > memoryStream.Length) return (null, 0, 0, 0, null);
                    }
                    else if (chunkId.ToLowerInvariant() == "data")
                    {
                        if (!fmtFound) { Console.WriteLine("Speechify WAV Error: 'data' chunk found before 'fmt '."); return (null, 0, 0, 0, null); }

                        if (chunkSize > memoryStream.Length - memoryStream.Position)
                        {
                            Console.WriteLine("Speechify WAV Error: data chunk size exceeds available data.");
                            chunkSize = (int)(memoryStream.Length - memoryStream.Position);
                            if (chunkSize < 0) chunkSize = 0;
                        }
                        pcmBuffer = reader.ReadBytes(chunkSize);
                        break;
                    }
                    else // Skip other chunks
                    {
                        if (memoryStream.Position < nextChunkPos && nextChunkPos <= memoryStream.Length)
                            reader.BaseStream.Seek(nextChunkPos - memoryStream.Position, SeekOrigin.Current);
                        else if (nextChunkPos > memoryStream.Length) return (null, 0, 0, 0, null);
                    }
                }

                if (pcmBuffer == null || !fmtFound)
                {
                    Console.WriteLine("Speechify WAV Error: 'data' or 'fmt ' chunk not found or PCM buffer is null.");
                    return (null, 0, 0, 0, null);
                }

                TimeSpan? duration = null;
                if (byteRate > 0 && pcmBuffer.Length > 0)
                {
                    duration = TimeSpan.FromSeconds((double)pcmBuffer.Length / byteRate);
                }
                else if (sampleRateFromWav > 0 && numChannels > 0 && bitsPerSampleFromWav > 0 && pcmBuffer.Length > 0)
                {
                    double bytesPerSampleCalc = bitsPerSampleFromWav / 8.0;
                    if (bytesPerSampleCalc == 0) return (pcmBuffer, sampleRateFromWav, numChannels, bitsPerSampleFromWav, TimeSpan.Zero);
                    double totalFrames = pcmBuffer.Length / (bytesPerSampleCalc * numChannels);
                    duration = TimeSpan.FromSeconds(totalFrames / sampleRateFromWav);
                }

                return (pcmBuffer, sampleRateFromWav, numChannels, bitsPerSampleFromWav, duration);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Speechify WAV Parsing Detailed Error: {ex.Message}");
                return (null, 0, 0, 0, null);
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

        public ITTSConfig GetCacheableConfig()
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