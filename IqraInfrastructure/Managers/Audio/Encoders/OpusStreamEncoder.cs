using Concentus;
using Concentus.Enums;
using Concentus.Structs;
using IqraCore.Entities.Helper.Audio;

namespace IqraInfrastructure.Managers.Audio.Encoders
{
    public class OpusStreamEncoder : BaseAudioStreamEncoder
    {
        private readonly IOpusEncoder _encoder;
        private readonly MemoryStream _buffer; // Accumulates MONO PCM bytes

        private readonly int _samplesPerFramePerChannel;
        private readonly int _monoBytesPerFrame;
        private readonly int _sampleRate;

        // WebRTC Standard: Stereo (2 Channels)
        private const int OPUS_CHANNELS = 2;

        public OpusStreamEncoder(int targetSampleRate, int frameDurationMs = 20)
            : base(AudioEncodingTypeEnum.OPUS, targetSampleRate, 16)
        {
            _sampleRate = targetSampleRate;

            // Valid Frame Sizes: 2.5, 5, 10, 20, 40, 60ms
            if (!new[] { 20, 40, 60 }.Contains(frameDurationMs))
            {
                throw new ArgumentException($"Unsupported Frame Duration: {frameDurationMs}ms");
            }

            // Initialize Encoder as STEREO
            _encoder = OpusCodecFactory.CreateEncoder(_sampleRate, OPUS_CHANNELS, OpusApplication.OPUS_APPLICATION_VOIP);
            _encoder.Bitrate = 32000; // todo make configurable??

            // Calculate Buffer Requirements based on MONO Input
            // We need to wait until we have enough Mono samples to create a full Stereo frame.
            // Duration is time-based, so samples count is constant regardless of channels for the *timeline*.
            _samplesPerFramePerChannel = _sampleRate * frameDurationMs / 1000;

            // Input is 16-bit (2 bytes) Mono (1 channel)
            _monoBytesPerFrame = _samplesPerFramePerChannel * 2 * 1;

            _buffer = new MemoryStream();
        }

        public override byte[] Encode(ReadOnlySpan<byte> pcmData, int inputSampleRate, int inputBitsPerSample)
        {
            // 1. Resample incoming data to match Encoder Rate (e.g. 24k -> 48k), still MONO, 16-bit
            var compatibleMonoPcm = ResampleAndFormat(pcmData, inputSampleRate, inputBitsPerSample, _sampleRate, 16);

            // 2. Add to Buffer
            _buffer.Write(compatibleMonoPcm, 0, compatibleMonoPcm.Length);

            // 3. Process Frames if we have enough data
            if (_buffer.Length >= _monoBytesPerFrame)
            {
                byte[] allBytes = _buffer.ToArray();
                int offset = 0;

                using var outputStream = new MemoryStream();

                // Buffers for conversion
                byte[] encodedBuffer = new byte[4096]; // Max Opus packet size
                short[] monoShorts = new short[_samplesPerFramePerChannel];
                short[] stereoShorts = new short[_samplesPerFramePerChannel * OPUS_CHANNELS];

                while ((allBytes.Length - offset) >= _monoBytesPerFrame)
                {
                    // A. Extract Mono Chunk
                    Buffer.BlockCopy(allBytes, offset, monoShorts, 0, _monoBytesPerFrame);

                    // B. Upmix Mono -> Stereo (Interleaved)
                    // L = Mono, R = Mono
                    for (int i = 0; i < _samplesPerFramePerChannel; i++)
                    {
                        short sample = monoShorts[i];
                        stereoShorts[i * 2] = sample;     // Left
                        stereoShorts[i * 2 + 1] = sample; // Right
                    }

                    // C. Encode
                    // Note: frameSize arg is samples per channel (not total samples)
                    int encodedLength = _encoder.Encode(stereoShorts, _samplesPerFramePerChannel, encodedBuffer, encodedBuffer.Length);

                    // D. Write encoded frame
                    outputStream.Write(encodedBuffer, 0, encodedLength);

                    offset += _monoBytesPerFrame;
                }

                // 4. Reset Buffer with leftovers
                _buffer.SetLength(0);
                if (offset < allBytes.Length)
                {
                    _buffer.Write(allBytes, offset, allBytes.Length - offset);
                }

                return outputStream.ToArray();
            }

            return Array.Empty<byte>();
        }

        public override void Dispose()
        {
            _buffer?.Dispose();
        }
    }
}