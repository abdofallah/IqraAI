using IqraCore.Entities.Helper.Audio;

namespace IqraCore.Interfaces.Audio
{
    public interface IAudioStreamEncoder : IDisposable
    {
        // Configuration
        AudioEncodingTypeEnum TargetEncoding { get; }
        int TargetSampleRate { get; }
        int TargetBitsPerSample { get; }

        /// <summary>
        /// Encodes a chunk of PCM audio.
        /// </summary>
        /// <param name="pcmData">Raw PCM data from the central engine.</param>
        /// <param name="inputSampleRate">The sample rate of the incoming pcmData.</param>
        /// <param name="inputBitsPerSample">The bits per sample of the incoming pcmData.</param>
        /// <returns>Encoded bytes (Opus frame, MuLaw bytes, or PCM). Returns empty array if buffering.</returns>
        byte[] Encode(ReadOnlySpan<byte> pcmData, int inputSampleRate, int inputBitsPerSample);
    }
}