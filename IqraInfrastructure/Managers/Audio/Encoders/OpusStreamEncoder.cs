using Concentus;
using Concentus.Enums;
using Concentus.Structs;
using IqraCore.Entities.Helper.Audio;

namespace IqraInfrastructure.Managers.Audio.Encoders
{
    public class OpusStreamEncoder : BaseAudioStreamEncoder
    {
        private readonly IOpusEncoder _encoder;
        private readonly MemoryStream _buffer; // To accumulate incoming PCM bytes
        private readonly int _frameSizeMs;
        private readonly int _samplesPerFrame;
        private readonly int _bytesPerFrame;

        // Opus usually works best at 48kHz, but supports 8, 12, 16, 24, 48.
        // We will default to 48kHz for WebRTC compatibility unless specified otherwise.
        private const int INTERNAL_OPUS_RATE = 48000;

        public OpusStreamEncoder(int targetSampleRate, int frameDurationMs = 60)
            : base(AudioEncodingTypeEnum.OPUS, targetSampleRate, 16)
        {
            // Valid Frame Sizes: 2.5, 5, 10, 20, 40, 60ms
            if (!new[] { 20, 40, 60 }.Contains(frameDurationMs))
                frameDurationMs = 60; // Default safe value

            _frameSizeMs = frameDurationMs;

            // Initialize Concentus Encoder
            // Application.Voip optimizes for speech.
            _encoder = OpusCodecFactory.CreateEncoder(INTERNAL_OPUS_RATE, 1, OpusApplication.OPUS_APPLICATION_VOIP);
            _encoder.Bitrate = 32000; // 32kbps is good for speech

            // Calculate Buffer Size requirements
            // Samples = Rate * Time. (e.g., 48000 * 0.060 = 2880 samples)
            _samplesPerFrame = INTERNAL_OPUS_RATE * _frameSizeMs / 1000;
            _bytesPerFrame = _samplesPerFrame * 2; // 16-bit = 2 bytes

            _buffer = new MemoryStream();
        }

        public override byte[] Encode(ReadOnlySpan<byte> pcmData, int inputSampleRate, int inputBitsPerSample)
        {
            // 1. Resample incoming data to Opus-compatible PCM (48kHz, 16-bit)
            // Even if target is 24k, we upsample to 48k for the internal encoder 
            // because Concentus works natively at 48k.
            var compatiblePcm = ResampleAndFormat(pcmData, inputSampleRate, inputBitsPerSample, INTERNAL_OPUS_RATE, 16);

            // 2. Add to Buffer
            _buffer.Write(compatiblePcm, 0, compatiblePcm.Length);

            // 3. Check if we have enough for a full frame
            if (_buffer.Length >= _bytesPerFrame)
            {
                byte[] allBytes = _buffer.ToArray();
                int offset = 0;
                using var outputStream = new MemoryStream();
                byte[] encodedBuffer = new byte[4096]; // Max Opus packet size
                short[] pcmShorts = new short[_samplesPerFrame];

                // Process all full frames in the buffer
                while ((allBytes.Length - offset) >= _bytesPerFrame)
                {
                    // Copy bytes to short[]
                    Buffer.BlockCopy(allBytes, offset, pcmShorts, 0, _bytesPerFrame);

                    // Encode
                    int encodedLength = _encoder.Encode(pcmShorts, _samplesPerFrame, encodedBuffer, encodedBuffer.Length);

                    // Write to output (Format: [Length (4 bytes)][Data])? 
                    // No, for simple streaming usually just the data, but for sticking packets together 
                    // the receiver needs to know boundaries. 
                    // For WebRTC/RTP, we return ONE packet per Encode call usually. 
                    // BUT, if we buffered multiple, we might return concatenated or just the last.
                    // STRATEGY: Return concatenated packets. The transport handles packetization.
                    outputStream.Write(encodedBuffer, 0, encodedLength);

                    offset += _bytesPerFrame;
                }

                // 4. Reset Buffer with leftovers
                _buffer.SetLength(0);
                if (offset < allBytes.Length)
                {
                    _buffer.Write(allBytes, offset, allBytes.Length - offset);
                }

                return outputStream.ToArray();
            }

            // Not enough data yet
            return Array.Empty<byte>();
        }

        public override void Dispose()
        {
            _buffer?.Dispose();
            // Concentus encoder is managed C#, no unmanaged resource to free explicitly
        }
    }
}
