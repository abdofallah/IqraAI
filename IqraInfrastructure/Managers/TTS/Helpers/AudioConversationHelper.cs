using Concentus;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.TTS;
using NAudio.Codecs;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NLayer.NAudioSupport;
using SIPSorceryMedia.Abstractions;

namespace IqraInfrastructure.Managers.TTS.Helpers
{
    public static class AudioConversationHelper
    {
        // NOTE: For G.729 support on Linux, the native SDL2 library must be installed.
        // On Debian/Ubuntu: sudo apt-get install -y libsdl2-2.0-0
        private static readonly SIPSorcery.Media.AudioEncoder _audioEncoder = new SIPSorcery.Media.AudioEncoder();

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
                // It's helpful to know which conversion failed.
                string errorMessage = $"An error occurred while converting audio from {sourceFormat.Encoding} ({sourceFormat.SampleRateHz}Hz) to {targetFormat.RequestedEncoding} ({targetFormat.RequestedSampleRateHz}Hz).";
                throw new Exception(errorMessage, ex);
            }
            finally
            {
                (resampler as IDisposable)?.Dispose();
                (sourceProvider as IDisposable)?.Dispose();
            }
        }

        /// <summary>
        /// Calculates the duration of audio data based on its format.
        /// </summary>
        public static TimeSpan CalculateDuration(byte[] audioData, TTSProviderAvailableAudioFormat format)
        {
            if (audioData == null || audioData.Length == 0) return TimeSpan.Zero;

            try
            {
                if (format.Encoding == AudioEncodingTypeEnum.WAV || IsWavFile(audioData))
                {
                    audioData = FixWavHeader(audioData);
                    using var reader = new WaveFileReader(new MemoryStream(audioData));
                    return reader.TotalTime;
                }

                // For MP3/MPEG, we must decode to get accurate duration.
                if (format.Encoding == AudioEncodingTypeEnum.MPEG)
                {
                    var inputStream = new MemoryStream(audioData);
                    var builder = new Mp3FileReader.FrameDecompressorBuilder(wf => new Mp3FrameDecompressor(wf));
                    using var reader = new Mp3FileReaderBase(inputStream, builder);
                    return reader.TotalTime;
                }

                if (format.SampleRateHz <= 0 || format.BitsPerSample <= 0) return TimeSpan.Zero;

                double bytesPerSample = format.BitsPerSample / 8.0;
                double totalSamples = audioData.Length / bytesPerSample;
                return TimeSpan.FromSeconds(totalSamples / format.SampleRateHz);
            }
            catch
            {
                return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Mixes two audio chunks, providing fine-grained control over volume and clipping.
        /// This method intelligently selects a high-performance path for PCM audio or a generic
        /// decode-mix-encode path for compressed formats.
        /// </summary>
        public static byte[] MixAudioChunks(
            AudioEncodingTypeEnum encodingType, int sampleRate, int bitsPerSample,
            ReadOnlyMemory<byte> speechChunk, float speechChunkVolume,
            ReadOnlyMemory<byte> backgroundChunk, float backgroundChunkVolume,
            float speechClipFactor = 1.0f, float backgroundClipFactor = 1.0f
        )
        {
            // --- Step 0: Handle Empty Chunks (Bug Fix) ---
            bool speechIsEmpty = speechChunk.IsEmpty || speechChunk.Length == 0;
            bool backgroundIsEmpty = backgroundChunk.IsEmpty || backgroundChunk.Length == 0;

            if (speechIsEmpty && backgroundIsEmpty) return Array.Empty<byte>();

            // --- Step 1: Dispatch to the Correct Mixing Strategy ---
            if (encodingType == AudioEncodingTypeEnum.PCM)
            {
                switch (bitsPerSample)
                {
                    case 8:
                        return MixPcm8BitChunks(speechChunk, speechChunkVolume, speechClipFactor, backgroundChunk, backgroundChunkVolume, backgroundClipFactor);
                    case 16:
                        return MixPcm16BitChunks(speechChunk, speechChunkVolume, speechClipFactor, backgroundChunk, backgroundChunkVolume, backgroundClipFactor);
                    case 24:
                        return MixPcm24BitChunks(speechChunk, speechChunkVolume, speechClipFactor, backgroundChunk, backgroundChunkVolume, backgroundClipFactor);
                }
            }

            // GENERIC PATH: For all other formats (32-bit PCM, G.729, Opus, etc.), use the decode-mix-encode pipeline.
            var sourceFormat = new TTSProviderAvailableAudioFormat { Encoding = encodingType, SampleRateHz = sampleRate, BitsPerSample = bitsPerSample };
            var pcmTargetFormat = new AudioRequestDetails { RequestedEncoding = AudioEncodingTypeEnum.PCM, RequestedSampleRateHz = sampleRate, RequestedBitsPerSample = 32 };

            var (speechPcm, _) = Convert(speechChunk.ToArray(), sourceFormat, pcmTargetFormat);
            var (backgroundPcm, _) = Convert(backgroundChunk.ToArray(), sourceFormat, pcmTargetFormat);

            byte[] mixedPcm = MixPcm32BitFloatChunks(speechPcm, speechChunkVolume, speechClipFactor, backgroundPcm, backgroundChunkVolume, backgroundClipFactor);

            var pcmSourceFormat = new TTSProviderAvailableAudioFormat { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = sampleRate, BitsPerSample = 32 };
            var finalTargetFormat = new AudioRequestDetails { RequestedEncoding = encodingType, RequestedSampleRateHz = sampleRate, RequestedBitsPerSample = bitsPerSample };

            var (finalMixedAudio, _) = Convert(mixedPcm, pcmSourceFormat, finalTargetFormat);
            return finalMixedAudio;
        }

        /// <summary>
        /// High-performance mixer for 8-bit unsigned PCM.
        /// </summary>
        private static byte[] MixPcm8BitChunks(ReadOnlyMemory<byte> speech, float speechVol, float speechClip, ReadOnlyMemory<byte> bg, float bgVol, float bgClip)
        {
            // Prevent division by zero
            if (speechClip < 0.1f) speechClip = 1.0f;
            if (bgClip < 0.1f) bgClip = 1.0f;

            var output = new byte[Math.Max(speech.Length, bg.Length)];
            var speechSpan = speech.Span;
            var bgSpan = bg.Span;

            for (int i = 0; i < output.Length; i++)
            {
                // Convert 8-bit unsigned (0 to 255) to a signed space for math
                int speechSample = i < speechSpan.Length ? (int)(((speechSpan[i] - 128) * speechVol) / speechClip) : 0;
                int bgSample = i < bgSpan.Length ? (int)(((bgSpan[i] - 128) * bgVol) / bgClip) : 0;

                int mixedSample = speechSample + bgSample;

                // Clamp in the signed space [-128, 127] and convert back to unsigned byte
                output[i] = (byte)(Math.Clamp(mixedSample, -128, 127) + 128);
            }
            return output;
        }

        /// <summary>
        /// High-performance mixer for 16-bit signed PCM.
        /// </summary>
        private static byte[] MixPcm16BitChunks(ReadOnlyMemory<byte> speech, float speechVol, float speechClip, ReadOnlyMemory<byte> bg, float bgVol, float bgClip)
        {
            if (speechClip < 0.1f) speechClip = 1.0f;
            if (bgClip < 0.1f) bgClip = 1.0f;

            var output = new byte[Math.Max(speech.Length, bg.Length)];
            var speechShorts = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, short>(speech.Span);
            var bgShorts = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, short>(bg.Span);
            var mixedShorts = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, short>(output.AsSpan());

            for (int i = 0; i < mixedShorts.Length; i++)
            {
                int speechSample = i < speechShorts.Length ? (int)((speechShorts[i] * speechVol) / speechClip) : 0;
                int bgSample = i < bgShorts.Length ? (int)((bgShorts[i] * bgVol) / bgClip) : 0;

                int mixedSample = speechSample + bgSample;
                mixedShorts[i] = (short)Math.Clamp(mixedSample, short.MinValue, short.MaxValue);
            }
            return output;
        }

        /// <summary>
        /// High-performance mixer for 24-bit signed PCM.
        /// </summary>
        private static byte[] MixPcm24BitChunks(ReadOnlyMemory<byte> speech, float speechVol, float speechClip, ReadOnlyMemory<byte> bg, float bgVol, float bgClip)
        {
            if (speechClip < 0.1f) speechClip = 1.0f;
            if (bgClip < 0.1f) bgClip = 1.0f;

            int maxLength = Math.Max(speech.Length, bg.Length);
            var output = new byte[maxLength - (maxLength % 3)]; // Ensure length is a multiple of 3
            var speechSpan = speech.Span;
            var bgSpan = bg.Span;

            int speechSampleCount = speech.Length / 3;
            int bgSampleCount = bg.Length / 3;
            int mixedSampleCount = output.Length / 3;

            for (int i = 0; i < mixedSampleCount; i++)
            {
                // Read 24-bit samples and convert to 32-bit int for math
                int speechSample = i < speechSampleCount ? Read24BitSample(speechSpan, i * 3) : 0;
                int bgSample = i < bgSampleCount ? Read24BitSample(bgSpan, i * 3) : 0;

                // Apply volume and clipping
                long processedSpeech = (long)((speechSample * speechVol) / speechClip);
                long processedBg = (long)((bgSample * bgVol) / bgClip);

                long mixedSample = processedSpeech + processedBg;

                // Clamp to 24-bit range and write back to output
                int clampedSample = (int)Math.Clamp(mixedSample, -8388608, 8388607);
                Write24BitSample(output, i * 3, clampedSample);
            }
            return output;
        }

        /// <summary>
        /// A mixer that operates on 32-bit float PCM byte arrays. Used by the generic path.
        /// </summary>
        private static byte[] MixPcm32BitFloatChunks(byte[] speechPcm, float speechVolume, float speechClip, byte[] backgroundPcm, float backgroundVolume, float bgClip)
        {
            if (speechClip < 0.1f) speechClip = 1.0f;
            if (bgClip < 0.1f) bgClip = 1.0f;

            using var mixedPcmStream = new MemoryStream();
            int speechIndex = 0;
            int backgroundIndex = 0;

            while (speechIndex < speechPcm.Length && backgroundIndex < backgroundPcm.Length)
            {
                float speechSample = BitConverter.ToSingle(speechPcm, speechIndex);
                float backgroundSample = BitConverter.ToSingle(backgroundPcm, backgroundIndex);

                float mixedSample = (speechSample * speechVolume / speechClip) + (backgroundSample * backgroundVolume / bgClip);
                mixedSample = Math.Clamp(mixedSample, -1.0f, 1.0f); // Clamp to valid float range

                mixedPcmStream.Write(BitConverter.GetBytes(mixedSample));
                speechIndex += 4;
                backgroundIndex += 4;
            }

            // Append the remainder of whichever chunk is longer
            if (speechIndex < speechPcm.Length) mixedPcmStream.Write(speechPcm, speechIndex, speechPcm.Length - speechIndex);
            else if (backgroundIndex < backgroundPcm.Length) mixedPcmStream.Write(backgroundPcm, backgroundIndex, backgroundPcm.Length - backgroundIndex);

            return mixedPcmStream.ToArray();
        }

        // --- 24-bit Helpers ---
        private static int Read24BitSample(ReadOnlySpan<byte> buffer, int offset)
        {
            int sample = buffer[offset] | (buffer[offset + 1] << 8) | (buffer[offset + 2] << 16);
            // Sign-extend if the 24th bit is set
            return (sample & 0x800000) != 0 ? sample | ~0xFFFFFF : sample;
        }

        private static void Write24BitSample(Span<byte> buffer, int offset, int sample)
        {
            buffer[offset] = (byte)(sample & 0xFF);
            buffer[offset + 1] = (byte)((sample >> 8) & 0xFF);
            buffer[offset + 2] = (byte)((sample >> 16) & 0xFF);
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
                    if (IsWavFile(sourceAudio))
                    {
                        sourceAudio = FixWavHeader(sourceAudio);
                        return new WaveFileReader(new MemoryStream(sourceAudio));
                    }
                    throw new ArgumentException("Source audio is not a WAV file.");

                case AudioEncodingTypeEnum.PCM:
                    if (IsWavFile(sourceAudio))
                    {
                        sourceAudio = FixWavHeader(sourceAudio);
                        return new WaveFileReader(new MemoryStream(sourceAudio));
                    }
                    var pcmFormat = new WaveFormat(sourceFormat.SampleRateHz, sourceFormat.BitsPerSample, 1);
                    return new RawSourceWaveStream(sourceAudio, 0, sourceAudio.Length, pcmFormat);

                case AudioEncodingTypeEnum.MULAW:
                    if (IsWavFile(sourceAudio))
                    {
                        sourceAudio = FixWavHeader(sourceAudio);
                        return new WaveFileReader(new MemoryStream(sourceAudio));
                    }
                    var mulawFormat = WaveFormat.CreateMuLawFormat(sourceFormat.SampleRateHz, 1);
                    return new RawSourceWaveStream(new MemoryStream(sourceAudio), mulawFormat);

                case AudioEncodingTypeEnum.ALAW:
                    if (IsWavFile(sourceAudio))
                    {
                        sourceAudio = FixWavHeader(sourceAudio);
                        return new WaveFileReader(new MemoryStream(sourceAudio));
                    }
                    var alawFormat = WaveFormat.CreateALawFormat(sourceFormat.SampleRateHz, 1);
                    return new RawSourceWaveStream(new MemoryStream(sourceAudio), alawFormat);

                case AudioEncodingTypeEnum.G722:
                    {
                        if (sourceFormat.SampleRateHz != 16000 || sourceFormat.BitsPerSample != 16)
                        {
                            throw new ArgumentException($"Invalid source format for G722. Expected metadata with 16000Hz/16bits, but received {sourceFormat.SampleRateHz}Hz/{sourceFormat.BitsPerSample} bits.");
                        }

                        var waveFormat = new WaveFormat(16000, 16, 1);
                        var codec = new G722Codec();
                        var state = new G722CodecState(64000, G722Flags.None);

                        var decodedBytes = new byte[sourceAudio.Length * 4];
                        var waveBuffer = new WaveBuffer(decodedBytes);

                        int samplesDecoded = codec.Decode(state, waveBuffer.ShortBuffer, sourceAudio, sourceAudio.Length);

                        return new RawSourceWaveStream(decodedBytes, 0, samplesDecoded * 2, waveFormat);
                    }
                
                case AudioEncodingTypeEnum.G729:
                    {
                        if (sourceFormat.SampleRateHz != 8000 || sourceFormat.BitsPerSample != 16)
                        {
                            throw new ArgumentException($"Invalid source format for G729. Expected metadata with 8000Hz/16bits, but received {sourceFormat.SampleRateHz}Hz/{sourceFormat.BitsPerSample} bits.");
                        }

                        var pcmShorts = _audioEncoder.DecodeAudio(sourceAudio, new AudioFormat(SDPWellKnownMediaFormatsEnum.G729));
                        var pcmBytes = new byte[pcmShorts.Length * sizeof(short)];
                        Buffer.BlockCopy(pcmShorts, 0, pcmBytes, 0, pcmBytes.Length);

                        var waveFormat = new WaveFormat(8000, 16, 1);
                        return new RawSourceWaveStream(pcmBytes, 0, pcmBytes.Length, waveFormat);
                    }

                case AudioEncodingTypeEnum.OPUS:
                    return DecodeFromOpus(sourceAudio, sourceFormat.SampleRateHz);

                case AudioEncodingTypeEnum.MPEG:
                    {
                        var inputStream = new MemoryStream(sourceAudio);
                        var builder = new Mp3FileReader.FrameDecompressorBuilder(wf => new Mp3FrameDecompressor(wf));
                        return new Mp3FileReaderBase(inputStream, builder);
                    }
                
                default:
                    throw new ArgumentException($"Unsupported source encoding type: {sourceFormat.Encoding}");
            }
        }

        /// <summary>
        /// Step 2: Creates a resampler to convert the source PCM to the target sample rate and bit depth.
        /// This now uses a cross-platform resampler.
        /// </summary>
        private static IWaveProvider CreateResampler(IWaveProvider sourceProvider, AudioRequestDetails targetFormat)
        {
            if (sourceProvider.WaveFormat.SampleRate == targetFormat.RequestedSampleRateHz)
            {
                if (sourceProvider.WaveFormat.BitsPerSample == targetFormat.RequestedBitsPerSample)
                {
                    return sourceProvider;
                }
            }

            var sampleProvider = sourceProvider.ToSampleProvider();

            var resampler = new WdlResamplingSampleProvider(sampleProvider, targetFormat.RequestedSampleRateHz);

            if (targetFormat.RequestedBitsPerSample == 32)
            {
                return resampler.ToWaveProvider();
            }
            else if (targetFormat.RequestedBitsPerSample == 24)
            {
                return new SampleToWaveProvider24(resampler);
            }
            else if (targetFormat.RequestedBitsPerSample == 16)
            {
                return resampler.ToWaveProvider16();
            }
            else if (targetFormat.RequestedBitsPerSample == 8)
            {
                return new SampleToWaveProvider8(resampler);
            }
            else
            {
                throw new ArgumentException($"Unsupported target PCM bit depth: {targetFormat.RequestedBitsPerSample}. Supported values: 8, 16, 24, 32.");
            }
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
                case AudioEncodingTypeEnum.WAV:
                    {
                        IWaveProvider finalPcmProvider = pcmProvider;
                        if (pcmProvider.WaveFormat.BitsPerSample != targetFormat.RequestedBitsPerSample)
                        {
                            finalPcmProvider = new WaveFormatConversionProvider(new WaveFormat(pcmProvider.WaveFormat.SampleRate, targetFormat.RequestedBitsPerSample, 1), pcmProvider);
                        }

                        if (targetFormat.RequestedEncoding == AudioEncodingTypeEnum.WAV)
                        {
                            using (var writer = new WaveFileWriter(ms, finalPcmProvider.WaveFormat))
                            {
                                finalPcmProvider.CopyTo(writer);
                            }
                        }
                        else // PCM
                        {
                            finalPcmProvider.CopyTo(ms);
                        }
                        return ms.ToArray();
                    }

                case AudioEncodingTypeEnum.MULAW:
                case AudioEncodingTypeEnum.ALAW:
                    {
                        var pcm16Provider = pcmProvider.WaveFormat.BitsPerSample == 16
                            ? pcmProvider
                            : pcmProvider.ToSampleProvider().ToWaveProvider16();

                        byte[] readBuffer = new byte[pcm16Provider.WaveFormat.BlockAlign * 1024];
                        int bytesRead;

                        while ((bytesRead = pcm16Provider.Read(readBuffer, 0, readBuffer.Length)) > 0)
                        {
                            for (int i = 0; i < bytesRead; i += 2)
                            {
                                short pcmSample = (short)(readBuffer[i] | readBuffer[i + 1] << 8);
                                byte encodedSample = targetFormat.RequestedEncoding == AudioEncodingTypeEnum.MULAW
                                    ? MuLawEncoder.LinearToMuLawSample(pcmSample)
                                    : ALawEncoder.LinearToALawSample(pcmSample);
                                ms.WriteByte(encodedSample);
                            }
                        }
                        return ms.ToArray();
                    }
                
                case AudioEncodingTypeEnum.G722:
                    {
                        if (pcmProvider.WaveFormat.SampleRate != 16000 || pcmProvider.WaveFormat.BitsPerSample != 16)
                        {
                            throw new InvalidOperationException($"G.722 encoding requires 16kHz, 16-bit PCM. The provided format is {pcmProvider.WaveFormat}. Resampling failed or was incorrect.");
                        }

                        var codec = new G722Codec();
                        var state = new G722CodecState(64000, G722Flags.None);
                        var pcmBuffer = new byte[320];
                        var g722Buffer = new byte[pcmBuffer.Length / 4];
                        int bytesRead;
                        while((bytesRead = pcmProvider.Read(pcmBuffer, 0, pcmBuffer.Length)) > 0)
                        {
                            var waveBuffer = new WaveBuffer(pcmBuffer);
                            int encodedCount = codec.Encode(state, g722Buffer, waveBuffer.ShortBuffer, bytesRead / 2);
                            ms.Write(g722Buffer, 0, encodedCount);
                        }
                        return ms.ToArray();
                    }
                
                case AudioEncodingTypeEnum.G729:
                    {
                        if (pcmProvider.WaveFormat.SampleRate != 8000 || pcmProvider.WaveFormat.BitsPerSample != 16)
                        {
                            throw new InvalidOperationException($"G.729 encoding requires 8kHz, 16-bit PCM. The provided format is {pcmProvider.WaveFormat}.");
                        }

                        var pcmBytes = ReadAllBytes(pcmProvider);
                        var pcmShorts = new short[pcmBytes.Length / 2];
                        Buffer.BlockCopy(pcmBytes, 0, pcmShorts, 0, pcmBytes.Length);

                        var g729Bytes = _audioEncoder.EncodeAudio(pcmShorts, new AudioFormat(SDPWellKnownMediaFormatsEnum.G729));
                        return g729Bytes;
                    }

                case AudioEncodingTypeEnum.OPUS:
                    return EncodeToOpus(pcmProvider, targetFormat);

                case AudioEncodingTypeEnum.MPEG:
                     throw new NotSupportedException($"Encoding to {targetFormat.RequestedEncoding} is not yet implemented. This requires an external library like LAME wrapped by NAudio.Lame.");
                
                default:
                    throw new ArgumentException($"Unsupported target encoding type: {targetFormat.RequestedEncoding}");
            }
        }

        // --- Codec-Specific and Utility Helpers ---

        private static IWaveProvider DecodeFromOpus(byte[] opusData, int sampleRate)
        {
            var decoder = OpusCodecFactory.CreateDecoder(sampleRate, 1);
            int maxFrameSize = sampleRate * 120 / 1000;
            var pcmBuffer = new short[maxFrameSize * 1]; // 1 channel
            int samplesDecoded = decoder.Decode(opusData, pcmBuffer, maxFrameSize, false);
            var pcmBytes = new byte[samplesDecoded * sizeof(short)];
            Buffer.BlockCopy(pcmBuffer, 0, pcmBytes, 0, pcmBytes.Length);
            var waveFormat = new WaveFormat(sampleRate, 16, 1);
            return new RawSourceWaveStream(pcmBytes, 0, pcmBytes.Length, waveFormat);
        }
        
        private static byte[] EncodeToOpus(IWaveProvider pcmProvider, AudioRequestDetails targetFormat)
        {
            int sampleRate = pcmProvider.WaveFormat.SampleRate;
            int frameSizeMs = 20; // Standard frame size
            int frameSizeSamples = sampleRate * frameSizeMs / 1000;
            
            var encoder = OpusCodecFactory.CreateEncoder(sampleRate, 1, Concentus.Enums.OpusApplication.OPUS_APPLICATION_VOIP);
            encoder.Bitrate = 32000;

            using var ms = new MemoryStream();
            var pcmShortBuffer = new short[frameSizeSamples];
            var readByteBuffer = new byte[frameSizeSamples * sizeof(short)];
            int bytesRead;

            while ((bytesRead = pcmProvider.Read(readByteBuffer, 0, readByteBuffer.Length)) > 0)
            {
                Buffer.BlockCopy(readByteBuffer, 0, pcmShortBuffer, 0, bytesRead);
                int samplesInFrame = bytesRead / sizeof(short);
                var encodedPacket = new byte[4000];
                int encodedLength = encoder.Encode(
                    pcmShortBuffer.AsSpan(0, samplesInFrame),
                    samplesInFrame,
                    encodedPacket.AsSpan(),
                    encodedPacket.Length
                );

                ms.Write(encodedPacket, 0, encodedLength);
            }
            return ms.ToArray();
        }

        private static byte[] ReadAllBytes(IWaveProvider provider)
        {
            using var ms = new MemoryStream();
            provider.CopyTo(ms);
            return ms.ToArray();
        }

        private static bool IsWavFile(byte[] data)
        {
            if (data == null || data.Length < 12) return false;
            return data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F' &&
                   data[8] == 'W' && data[9] == 'A' && data[10] == 'V' && data[11] == 'E';
        }
        
        private static byte[] FixWavHeader(byte[] wavData)
        {
            if (!IsWavFile(wavData) || wavData.Length < 44) return wavData;
            
            int correctFileSize = wavData.Length - 8;
            Array.Copy(BitConverter.GetBytes(correctFileSize), 0, wavData, 4, 4);

            int dataChunkOffset = -1;
            for (int i = 12; i < wavData.Length - 8; i++)
            {
                if (wavData[i] == 'd' && wavData[i + 1] == 'a' && wavData[i + 2] == 't' && wavData[i + 3] == 'a')
                {
                    dataChunkOffset = i;
                    break;
                }
            }

            if (dataChunkOffset != -1)
            {
                int dataChunkSizeOffset = dataChunkOffset + 4;
                int correctDataSize = wavData.Length - (dataChunkOffset + 8);
                Array.Copy(BitConverter.GetBytes(correctDataSize), 0, wavData, dataChunkSizeOffset, 4);
            }
            return wavData;
        }

        #endregion

        #region Private Nested Class for 8/24-Bit Conversion

        /// <summary>
        /// A custom IWaveProvider that converts a 32-bit floating-point ISampleProvider
        /// into an 8-bit unsigned PCM IWaveProvider on the fly.
        /// </summary>
        private class SampleToWaveProvider8 : IWaveProvider
        {
            private readonly ISampleProvider _sourceProvider;
            private readonly WaveFormat _waveFormat;
            private float[] _sourceBuffer;

            public SampleToWaveProvider8(ISampleProvider sourceProvider)
            {
                if (sourceProvider.WaveFormat.Channels != 1)
                {
                    throw new NotSupportedException("Only mono audio is supported for 8-bit conversion.");
                }

                _sourceProvider = sourceProvider;
                _waveFormat = new WaveFormat(sourceProvider.WaveFormat.SampleRate, 8, 1);
            }

            public WaveFormat WaveFormat => _waveFormat;

            public int Read(byte[] buffer, int offset, int count)
            {
                // The number of float samples to read is the same as the number of bytes to write.
                int samplesToRead = count;

                // Ensure our source buffer is large enough
                if (_sourceBuffer == null || _sourceBuffer.Length < samplesToRead)
                {
                    _sourceBuffer = new float[samplesToRead];
                }

                // Read from the 32-bit float source provider
                int samplesRead = _sourceProvider.Read(_sourceBuffer, 0, samplesToRead);

                // Convert each float sample to an 8-bit byte
                for (int i = 0; i < samplesRead; i++)
                {
                    // The formula to convert a float sample from [-1.0, 1.0] to a byte [0, 255]
                    float sample = _sourceBuffer[i];
                    // Clamp the sample to the valid range to prevent issues from rogue samples
                    if (sample > 1.0f) sample = 1.0f;
                    if (sample < -1.0f) sample = -1.0f;

                    buffer[offset + i] = (byte)((sample + 1.0f) * 127.5f);
                }

                return samplesRead;
            }
        }

        /// <summary>
        /// A custom IWaveProvider that converts a 32-bit floating-point ISampleProvider
        /// into a 24-bit signed PCM IWaveProvider on the fly.
        /// </summary>
        private class SampleToWaveProvider24 : IWaveProvider
        {
            private readonly ISampleProvider _sourceProvider;
            private readonly WaveFormat _waveFormat;
            private float[] _sourceBuffer;

            // The maximum positive value for a 24-bit signed integer.
            private const int MaxValue24Bit = 8388607; // 2^23 - 1

            public SampleToWaveProvider24(ISampleProvider sourceProvider)
            {
                if (sourceProvider.WaveFormat.Channels != 1)
                {
                    throw new NotSupportedException("Only mono audio is supported for 24-bit conversion.");
                }

                _sourceProvider = sourceProvider;
                _waveFormat = new WaveFormat(sourceProvider.WaveFormat.SampleRate, 24, 1);
            }

            public WaveFormat WaveFormat => _waveFormat;

            public int Read(byte[] buffer, int offset, int count)
            {
                // Calculate how many 3-byte samples we need to generate.
                int samplesToRead = count / 3;

                // Ensure our source float buffer is large enough.
                if (_sourceBuffer == null || _sourceBuffer.Length < samplesToRead)
                {
                    _sourceBuffer = new float[samplesToRead];
                }

                // Read from the 32-bit float source provider.
                int samplesRead = _sourceProvider.Read(_sourceBuffer, 0, samplesToRead);

                int bufferIndex = offset;
                for (int i = 0; i < samplesRead; i++)
                {
                    // Get the float sample and clamp it to the valid range.
                    float sample = _sourceBuffer[i];
                    if (sample > 1.0f) sample = 1.0f;
                    if (sample < -1.0f) sample = -1.0f;

                    // Convert the float sample to a 24-bit integer value.
                    int pcm24 = (int)(sample * MaxValue24Bit);

                    // Write the 3 bytes of the 24-bit sample in little-endian order.
                    buffer[bufferIndex++] = (byte)(pcm24 & 0xFF);         // Least significant byte
                    buffer[bufferIndex++] = (byte)((pcm24 >> 8) & 0xFF);  // Middle byte
                    buffer[bufferIndex++] = (byte)((pcm24 >> 16) & 0xFF); // Most significant byte
                }

                // Return the total number of bytes written.
                return samplesRead * 3;
            }
        }

        #endregion
    }

    // Helper extension methods to simplify stream copying
    internal static class WaveProviderExtensions
    {
        public static void CopyTo(this IWaveProvider source, Stream destination)
        {
            byte[] buffer = new byte[source.WaveFormat.AverageBytesPerSecond];
            int bytesRead;
            while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                destination.Write(buffer, 0, bytesRead);
            }
        }
    }
}