using Microsoft.Extensions.Logging;
using NAudio.Wave;
using NLayer.NAudioSupport;

namespace IqraCore.Utilities.Audio
{
    public static class AudioToRAWPCMConverter
    {
        public static async Task<ReadOnlyMemory<byte>> ConvertToRawPcmAsync(
            ReadOnlyMemory<byte> originalData,
            string contentType,
            string fileIdForLogging,
            ILogger logger,
            string agentId,
            int targetSampleRate,
            int targetBitsPerSample,
            int targetChannels)
        {
            // Basic input check
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (string.IsNullOrEmpty(agentId)) throw new ArgumentNullException(nameof(agentId));
            if (targetSampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(targetSampleRate));
            if (targetBitsPerSample <= 0 || targetBitsPerSample % 8 != 0) throw new ArgumentOutOfRangeException(nameof(targetBitsPerSample), "Must be a positive multiple of 8.");
            if (targetChannels <= 0) throw new ArgumentOutOfRangeException(nameof(targetChannels));


            try
            {
                // NAudio often works better with arrays for MemoryStream input in some scenarios
                using var inputStream = new MemoryStream(originalData.ToArray());
                WaveStream? reader = null;

                // --- Create appropriate reader based on ContentType ---
                if (contentType.Equals("audio/wav", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        reader = new WaveFileReader(inputStream);
                        logger.LogDebug("Agent {AgentId}: Reading WAV file (ID: {FileId}) for conversion.", agentId, fileIdForLogging);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Agent {AgentId}: Failed to initialize WaveFileReader for WAV file (ID: {FileId})", agentId, fileIdForLogging);
                        return ReadOnlyMemory<byte>.Empty;
                    }
                }
                else if (contentType.Equals("audio/mpeg", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var builder = new Mp3FileReader.FrameDecompressorBuilder(wf => new Mp3FrameDecompressor(wf));
                        reader = new Mp3FileReaderBase(inputStream, builder);
                        logger.LogDebug("Agent {AgentId}: Reading MP3 file (ID: {FileId}) for conversion.", agentId, fileIdForLogging);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Agent {AgentId}: Failed to initialize Mp3FileReader for MP3 file (ID: {FileId})", agentId, fileIdForLogging);
                        return ReadOnlyMemory<byte>.Empty;
                    }
                }
                else
                {
                    logger.LogWarning("Agent {AgentId}: Unsupported content type '{ContentType}' for background audio (ID: {FileId}).", agentId, contentType, fileIdForLogging);
                    return ReadOnlyMemory<byte>.Empty;
                }

                if (reader == null)
                {
                    logger.LogWarning("Agent {AgentId}: Could not create a valid audio reader for ID {FileId} with ContentType {ContentType}.", agentId, fileIdForLogging, contentType);
                    return ReadOnlyMemory<byte>.Empty;
                }

                // --- Configure Resampling and Format Conversion ---
                using (reader)
                {
                    // --- Use parameters to create targetFormat ---
                    var targetFormat = new WaveFormat(targetSampleRate, targetBitsPerSample, targetChannels);
                    logger.LogDebug("Agent {AgentId}: Target conversion format for ID {FileId}: {SampleRate}Hz, {BitsPerSample}bit, {Channels}ch PCM",
                        agentId, fileIdForLogging, targetSampleRate, targetBitsPerSample, targetChannels);


                    // Use MediaFoundationResampler
                    using var resampler = new MediaFoundationResampler(reader, targetFormat);

                    // Check output format encoding
                    WaveFormat actualOutputFormat = resampler.WaveFormat;
                    if (actualOutputFormat.Encoding != WaveFormatEncoding.Pcm)
                    {
                        // MFR might output IEEE float if targetBitsPerSample is 32.
                        // If the target was 16-bit PCM, this check is important.
                        if (targetBitsPerSample == 16 && actualOutputFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                        {
                            logger.LogWarning("Agent {AgentId}: Resampler output is IEEE Float unexpectedly for ID {FileId}, attempting further conversion to PCM 16-bit.", agentId, fileIdForLogging);
                            // You might need an additional conversion step here if MFR outputs float when you expected 16-bit PCM.
                            // This usually doesn't happen if targetFormat explicitly requests 16-bit PCM.
                            // Example using SampleToWaveProvider16 (needs using NAudio.Wave.SampleProviders;)
                            // using var floatToPcm = new SampleToWaveProvider16(resampler.ToSampleProvider());
                            // await floatToPcm.CopyToAsync(outputStream); // Read from floatToPcm instead
                            // For simplicity, we'll error out for now if it's not PCM.
                            logger.LogError("Agent {AgentId}: Resampler output is unexpectedly IEEE Float for ID {FileId} when {TargetBitsPerSample}-bit PCM was requested.", agentId, fileIdForLogging, targetBitsPerSample);
                            return ReadOnlyMemory<byte>.Empty;

                        }
                        else if (actualOutputFormat.Encoding != WaveFormatEncoding.Pcm)
                        {
                            logger.LogError("Agent {AgentId}: Resampler output is not PCM for ID {FileId}. Actual Encoding: {Encoding}", agentId, fileIdForLogging, actualOutputFormat.Encoding);
                            return ReadOnlyMemory<byte>.Empty;
                        }
                    }
                    // Verify the actual output format matches the target (optional but good sanity check)
                    if (actualOutputFormat.SampleRate != targetSampleRate || actualOutputFormat.BitsPerSample != targetBitsPerSample || actualOutputFormat.Channels != targetChannels)
                    {
                        logger.LogWarning("Agent {AgentId}: Resampler output format ({ARate}Hz {ABits}bit {AChan}ch {AEnc}) differs slightly from target ({TRate}Hz {TBits}bit {TChan}ch PCM) for ID {FileId}. Proceeding with actual format.",
                           agentId, actualOutputFormat.SampleRate, actualOutputFormat.BitsPerSample, actualOutputFormat.Channels, actualOutputFormat.Encoding,
                           targetSampleRate, targetBitsPerSample, targetChannels, fileIdForLogging);
                        // Continue, but be aware the final bytes might not be *exactly* the requested format if MFR couldn't achieve it.
                    }


                    // --- Read Converted Data ---
                    using var outputStream = new MemoryStream();
                    // Use a buffer size appropriate for the *output* format
                    byte[] buffer = new byte[actualOutputFormat.AverageBytesPerSecond]; // Read ~1 second at a time
                    int bytesRead;
                    while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        await outputStream.WriteAsync(buffer, 0, bytesRead);
                    }

                    if (outputStream.Length == 0)
                    {
                        logger.LogWarning("Agent {AgentId}: Conversion resulted in zero bytes for ID {FileId}.", agentId, fileIdForLogging);
                    }
                    else
                    {
                        logger.LogDebug("Agent {AgentId}: Conversion successful for ID {FileId}. Output PCM length: {Length} bytes.", agentId, fileIdForLogging, outputStream.Length);
                    }

                    return new ReadOnlyMemory<byte>(outputStream.ToArray());
                }
            }
            catch (InvalidDataException ide)
            {
                logger.LogError(ide, "Agent {AgentId}: Invalid audio data encountered during conversion for ID {FileId}. ContentType: {ContentType}", agentId, fileIdForLogging, contentType);
                return ReadOnlyMemory<byte>.Empty;
            }
            catch (NotSupportedException nse)
            {
                logger.LogError(nse, "Agent {AgentId}: Audio format/conversion not supported by NAudio/MediaFoundation for ID {FileId}. ContentType: {ContentType}. Ensure Media Foundation is available.", agentId, fileIdForLogging, contentType);
                return ReadOnlyMemory<byte>.Empty;
            }
            catch (System.Runtime.InteropServices.COMException comEx)
            {
                logger.LogError(comEx, "Agent {AgentId}: COM error during Media Foundation conversion for ID {FileId}. ContentType: {ContentType}. HResult: {HResult}", agentId, fileIdForLogging, contentType, comEx.HResult);
                return ReadOnlyMemory<byte>.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Agent {AgentId}: Generic error during audio conversion for ID {FileId}. ContentType: {ContentType}", agentId, fileIdForLogging, contentType);
                return ReadOnlyMemory<byte>.Empty;
            }
        }
    }
}
