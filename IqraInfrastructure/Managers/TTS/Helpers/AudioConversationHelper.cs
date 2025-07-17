using Concentus;
using Concentus.Enums;
using Concentus.Structs;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.TTS;
using NAudio.Wave;

namespace IqraInfrastructure.Managers.TTS.Helpers
{
    public static class AudioConversationHelper
    {
        /// <summary>
        /// Converts audio data from a source format to a target format, handling encoding, sample rate, and bit depth changes.
        /// </summary>
        public static (byte[] audioData, TimeSpan duration) Convert(byte[] sourceAudio, TTSProviderAvailableAudioFormat sourceFormat, AudioRequestDetails targetFormat)
        {
            if (sourceAudio == null || sourceAudio.Length == 0)
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }

            // Optimization: If no conversion is needed at all.
            if (sourceFormat.Encoding == targetFormat.RequestedEncoding &&
                sourceFormat.SampleRateHz == targetFormat.RequestedSampleRateHz &&
                sourceFormat.BitsPerSample == targetFormat.RequestedBitsPerSample)
            {
                return (sourceAudio, CalculateDuration(sourceAudio, sourceFormat));
            }

            IWaveProvider? sourceProvider = null;
            IWaveProvider? resampler = null;
            try
            {
                sourceProvider = CreateWaveProvider(sourceAudio, sourceFormat);
                resampler = CreateResampler(sourceProvider, targetFormat);

                byte[] targetAudio = EncodeToTargetFormat(resampler, targetFormat);

                // Calculate duration on the final, converted data.
                var finalFormat = new TTSProviderAvailableAudioFormat
                {
                    Encoding = targetFormat.RequestedEncoding,
                    SampleRateHz = targetFormat.RequestedSampleRateHz,
                    BitsPerSample = targetFormat.RequestedBitsPerSample
                };

                TimeSpan duration = CalculateDuration(targetAudio, finalFormat);

                return (targetAudio, duration);
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while converting audio data.", ex);
            }
            finally
            {
                (resampler as IDisposable)?.Dispose();
                (sourceProvider as IDisposable)?.Dispose();
            }
            // --- END OF CORRECTION ---
        }

        /// <summary>
        /// Calculates the duration of audio data based on its format.
        /// </summary>
        public static TimeSpan CalculateDuration(byte[] audioData, TTSProviderAvailableAudioFormat format)
        {
            if (audioData == null || audioData.Length == 0) return TimeSpan.Zero;

            try
            {
                // For WAV files, the header is the most reliable source of duration.
                if (format.Encoding == AudioEncodingTypeEnum.WAV || IsWavFile(audioData))
                {
                    audioData = FixWavHeader(audioData);
                    using var reader = new WaveFileReader(new MemoryStream(audioData));
                    return reader.TotalTime;
                }

                // For raw audio formats.
                if (format.SampleRateHz <= 0 || format.BitsPerSample <= 0) return TimeSpan.Zero;

                double bytesPerSample = format.BitsPerSample / 8.0;
                // Assuming mono audio, which is standard for these TTS/telephony use cases.
                double totalSamples = audioData.Length / bytesPerSample;
                return TimeSpan.FromSeconds(totalSamples / format.SampleRateHz);
            }
            catch
            {
                // If parsing fails, return zero.
                return TimeSpan.Zero;
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Step 1: Creates an NAudio IWaveProvider (a raw PCM stream) from the source audio bytes.
        /// </summary>
        private static IWaveProvider CreateWaveProvider(byte[] sourceAudio, TTSProviderAvailableAudioFormat sourceFormat)
        {
            switch (sourceFormat.Encoding)
            {
                case AudioEncodingTypeEnum.WAV:
                    return new WaveFileReader(new MemoryStream(sourceAudio));

                case AudioEncodingTypeEnum.PCM:
                    // Handle case where PCM is actually in a WAV container (from Hamsa etc.)
                    if (IsWavFile(sourceAudio))
                    {
                        sourceAudio = FixWavHeader(sourceAudio);
                        return new WaveFileReader(new MemoryStream(sourceAudio));
                    }
                    var pcmFormat = new WaveFormat(sourceFormat.SampleRateHz, sourceFormat.BitsPerSample, 1);
                    return new RawSourceWaveStream(sourceAudio, 0, sourceAudio.Length, pcmFormat);

                case AudioEncodingTypeEnum.MULAW:
                    {
                        if (IsWavFile(sourceAudio)) // Handle WAV-contained MuLaw
                        {
                            sourceAudio = FixWavHeader(sourceAudio);
                            return new WaveFileReader(new MemoryStream(sourceAudio));
                        }
                        var mulawFormat = WaveFormat.CreateMuLawFormat(sourceFormat.SampleRateHz, 1);
                        return new RawSourceWaveStream(sourceAudio, 0, sourceAudio.Length, mulawFormat);
                    }

                case AudioEncodingTypeEnum.ALAW:
                    if (IsWavFile(sourceAudio)) // Handle WAV-contained Alaw
                    {
                        sourceAudio = FixWavHeader(sourceAudio);
                        return new WaveFileReader(new MemoryStream(sourceAudio));
                    }
                    var alawFormat = WaveFormat.CreateALawFormat(sourceFormat.SampleRateHz, 1);
                    return new RawSourceWaveStream(new MemoryStream(sourceAudio), alawFormat);

                case AudioEncodingTypeEnum.OPUS:
                    return DecodeFromOpus(sourceAudio, sourceFormat.SampleRateHz);

                case AudioEncodingTypeEnum.G722:
                case AudioEncodingTypeEnum.G729:
                    throw new NotSupportedException($"Decoding for {sourceFormat.Encoding} is not built-in. A dedicated library wrapper is required.");

                default:
                    throw new ArgumentException($"Unsupported source encoding type: {sourceFormat.Encoding}");
            }
        }

        /// <summary>
        /// Step 2: Creates a resampler to convert the source PCM to the target sample rate and bit depth.
        /// </summary>
        private static IWaveProvider CreateResampler(IWaveProvider sourceProvider, AudioRequestDetails targetFormat)
        {
            // The target format for our intermediate PCM stream
            var targetPcmWaveFormat = new WaveFormat(targetFormat.RequestedSampleRateHz, targetFormat.RequestedBitsPerSample, 1);

            // If the source is already in the correct PCM format, no resampling is needed.
            if (sourceProvider.WaveFormat.Equals(targetPcmWaveFormat))
            {
                return sourceProvider;
            }

            // Use MediaFoundationResampler for high-quality resampling.
            return new MediaFoundationResampler(sourceProvider, targetPcmWaveFormat);
        }

        /// <summary>
        /// Step 3: Reads all bytes from the resampled PCM provider and encodes them into the final target format.
        /// </summary>
        private static byte[] EncodeToTargetFormat(IWaveProvider pcmProvider, AudioRequestDetails targetFormat)
        {
            using var ms = new MemoryStream();
            switch (targetFormat.RequestedEncoding)
            {
                case AudioEncodingTypeEnum.PCM:
                    {
                        var buffer = new byte[8192];
                        int bytesRead;
                        while ((bytesRead = pcmProvider.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ms.Write(buffer, 0, bytesRead);
                        }
                        return ms.ToArray();
                    }

                case AudioEncodingTypeEnum.WAV:
                    {
                        using (var writer = new WaveFileWriter(ms, pcmProvider.WaveFormat))
                        {
                            var wavBuffer = new byte[8192];
                            int read;
                            while ((read = pcmProvider.Read(wavBuffer, 0, wavBuffer.Length)) > 0)
                            {
                                writer.Write(wavBuffer, 0, read);
                            }
                        }
                        return ms.ToArray();
                    }

                case AudioEncodingTypeEnum.MULAW:
                case AudioEncodingTypeEnum.ALAW:
                    {
                        byte[] readBuffer = new byte[pcmProvider.WaveFormat.BlockAlign * 1024];
                        int bytesRead;

                        while ((bytesRead = pcmProvider.Read(readBuffer, 0, readBuffer.Length)) > 0)
                        {
                            // Iterate through the 16-bit PCM samples
                            for (int i = 0; i < bytesRead; i += 2)
                            {
                                short pcmSample = (short)(readBuffer[i] | readBuffer[i + 1] << 8);
                                byte encodedSample;

                                if (targetFormat.RequestedEncoding == AudioEncodingTypeEnum.MULAW)
                                {
                                    encodedSample = NAudio.Codecs.MuLawEncoder.LinearToMuLawSample(pcmSample);
                                }
                                else // ALAW
                                {
                                    encodedSample = NAudio.Codecs.ALawEncoder.LinearToALawSample(pcmSample);
                                }
                                ms.WriteByte(encodedSample);
                            }
                        }

                        return ms.ToArray();
                    }

                case AudioEncodingTypeEnum.OPUS:
                    return EncodeToOpus(pcmProvider);

                case AudioEncodingTypeEnum.G722:
                case AudioEncodingTypeEnum.G729:
                    throw new NotSupportedException($"Encoding for {targetFormat.RequestedEncoding} is not built-in. A dedicated library wrapper is required.");

                default:
                    throw new ArgumentException($"Unsupported target encoding type: {targetFormat.RequestedEncoding}");
            }
        }

        // --- Codec-Specific Helpers ---
        private static IWaveProvider DecodeFromOpus(byte[] opusData, int sampleRate)
        {
            // Use the recommended factory to create the decoder instance.
            var decoder = OpusCodecFactory.CreateDecoder(sampleRate, 1, null);

            // From the Concentus docs, the maximum frame size is 120ms.
            // We create a buffer large enough to hold the maximum possible decoded PCM data from one packet.
            int maxFrameSize = sampleRate * 120 / 1000;
            var pcmBuffer = new short[maxFrameSize];

            // Decode the entire Opus packet into the PCM buffer.
            // For most TTS/API use cases, the opusData byte array represents a single logical packet.
            int samplesDecoded = decoder.Decode(opusData, pcmBuffer, maxFrameSize, false);

            // Create the final byte array, but only from the portion of the buffer that was actually filled.
            var pcmBytes = new byte[samplesDecoded * sizeof(short)];
            Buffer.BlockCopy(pcmBuffer, 0, pcmBytes, 0, pcmBytes.Length);

            var waveFormat = new WaveFormat(sampleRate, 16, 1);
            return new RawSourceWaveStream(pcmBytes, 0, pcmBytes.Length, waveFormat);
        }

        private static byte[] EncodeToOpus(IWaveProvider pcmProvider)
        {
            int sampleRate = pcmProvider.WaveFormat.SampleRate;
            // Use a standard 20ms frame size for VoIP applications.
            int frameSizeInSamples = sampleRate * 20 / 1000;

            var encoder = OpusCodecFactory.CreateEncoder(sampleRate, 1, OpusApplication.OPUS_APPLICATION_VOIP, null);
            encoder.Bitrate = 32000; // Set a reasonable default bitrate for voice.

            using var ms = new MemoryStream();

            // Create a buffer for the PCM data in the required 'short' format.
            var pcmShortBuffer = new short[frameSizeInSamples];
            // Create a byte buffer to read from the source provider. Its size corresponds to the short buffer.
            var readByteBuffer = new byte[frameSizeInSamples * sizeof(short)];

            int bytesRead;

            // Read from the source provider into the byte buffer.
            while ((bytesRead = pcmProvider.Read(readByteBuffer, 0, readByteBuffer.Length)) > 0)
            {
                // Convert the bytes read into shorts.
                Buffer.BlockCopy(readByteBuffer, 0, pcmShortBuffer, 0, bytesRead);

                // Calculate how many samples we actually have in this frame.
                int samplesInFrame = bytesRead / sizeof(short);

                // A buffer large enough for any encoded packet.
                var encodedPacket = new byte[4000];

                // Pass the short span, the number of samples, the output span, and the max output size.
                int encodedLength = encoder.Encode(
                    pcmShortBuffer.AsSpan(0, samplesInFrame),
                    samplesInFrame,
                    encodedPacket.AsSpan(),
                    encodedPacket.Length);

                // Write the successfully encoded packet to the memory stream.
                ms.Write(encodedPacket, 0, encodedLength);
            }
            return ms.ToArray();
        }

        private static bool IsWavFile(byte[] data)
        {
            if (data == null || data.Length < 12) return false;
            return data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F' &&
                   data[8] == 'W' && data[9] == 'A' && data[10] == 'V' && data[11] == 'E';
        }

        /// <summary>
        /// Manually corrects the RIFF and 'data' chunk sizes in a WAV file's header.
        /// Many streaming APIs write placeholder values (like 0 or -1) which causes NAudio to fail.
        /// </summary>
        /// <param name="wavData">The byte array containing the potentially malformed WAV data.</param>
        /// <returns>A byte array with a corrected WAV header.</returns>
        private static byte[] FixWavHeader(byte[] wavData)
        {
            // If it's not a WAV file or is too small to have a header, return it as is.
            if (!IsWavFile(wavData) || wavData.Length < 44)
            {
                return wavData;
            }

            // Fix 1: The RIFF chunk size (overall file size).
            // This should be the total length of the file minus 8 bytes (for "RIFF" and this size field).
            int correctFileSize = wavData.Length - 8;
            byte[] fileSize_bytes = BitConverter.GetBytes(correctFileSize);
            // Write the corrected bytes to the header (offset 4).
            Array.Copy(fileSize_bytes, 0, wavData, 4, 4);

            // Fix 2: The 'data' sub-chunk size.
            // We need to find the 'data' chunk, as other chunks like 'fmt ' or 'fact' can come first.
            int dataChunkOffset = -1;
            for (int i = 12; i < wavData.Length - 8; i++) // Start searching after "WAVE"
            {
                if (wavData[i] == 'd' && wavData[i + 1] == 'a' && wavData[i + 2] == 't' && wavData[i + 3] == 'a')
                {
                    dataChunkOffset = i;
                    break;
                }
            }

            if (dataChunkOffset != -1)
            {
                // The data chunk size is at an offset of 4 bytes from the "data" marker.
                int dataChunkSizeOffset = dataChunkOffset + 4;
                // The correct size is the total file length minus the position of the start of the data.
                int correctDataSize = wavData.Length - (dataChunkOffset + 8);
                byte[] dataSize_bytes = BitConverter.GetBytes(correctDataSize);
                Array.Copy(dataSize_bytes, 0, wavData, dataChunkSizeOffset, 4);
            }

            // Return the modified byte array.
            return wavData;
        }

        #endregion
    }
}