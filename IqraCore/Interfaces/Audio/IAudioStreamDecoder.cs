using IqraCore.Entities.Helper.Audio;

namespace IqraCore.Interfaces.Audio.Decoders
{
    public interface IAudioStreamDecoder : IDisposable
    {
        /// <summary>
        /// The format expected from the client (Input Format).
        /// </summary>
        AudioEncodingTypeEnum InputEncoding { get; }
        int InputSampleRate { get; }
        int InputBitsPerSample { get; }

        /// <summary>
        /// Decodes incoming raw bytes into the system-standard PCM format (16kHz, 32-bit).
        /// </summary>
        /// <param name="inputData">Raw bytes from the transport (e.g., Opus frame, MuLaw bytes).</param>
        /// <returns>PCM 16kHz 32-bit bytes ready for the Agent/STT.</returns>
        byte[] Decode(ReadOnlySpan<byte> inputData);
    }
}