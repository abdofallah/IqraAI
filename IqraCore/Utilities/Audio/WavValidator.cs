using System.Text;

namespace IqraCore.Utilities
{
    public class WavValidator
    {
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; }
            public TimeSpan Duration { get; set; }
            public WavMetadata Metadata { get; set; }

            public static ValidationResult Success(TimeSpan duration, WavMetadata metadata) =>
                new ValidationResult { IsValid = true, Duration = duration, Metadata = metadata };

            public static ValidationResult Error(string message) =>
                new ValidationResult { IsValid = false, ErrorMessage = message };
        }

        public class WavMetadata
        {
            public int SampleRate { get; set; }
            public short Channels { get; set; }
            public short BitsPerSample { get; set; }
            public string AudioFormat { get; set; }
            public int FileSize { get; set; }
        }

        public ValidationResult ValidateFile(string filePath)
        {
            try
            {
                using (var fileStream = File.OpenRead(filePath))
                {
                    return ValidateStream(fileStream);
                }
            }
            catch (Exception ex)
            {
                return ValidationResult.Error($"Error opening WAV file: {ex.Message}");
            }
        }

        public ValidationResult ValidateStream(Stream stream)
        {
            if (stream == null)
                return ValidationResult.Error("Stream is null");

            if (!stream.CanRead)
                return ValidationResult.Error("Stream is not readable");

            // Store original position to restore later if needed
            long originalPosition = stream.Position;

            try
            {
                using (var reader = new BinaryReader(stream, Encoding.ASCII, true))  // leaveOpen: true
                {
                    // Check stream size
                    if (stream.Length < 44) // Minimum WAV header size
                        return ValidationResult.Error("Stream is too small to be a valid WAV file");

                    // Validate RIFF header
                    string riffHeader = Encoding.ASCII.GetString(reader.ReadBytes(4));
                    if (riffHeader != "RIFF")
                        return ValidationResult.Error("Invalid RIFF header");

                    // Get file size from header
                    int fileSize = reader.ReadInt32() + 8; // Total size including 8 bytes of RIFF header
                    if (fileSize != stream.Length)
                        return ValidationResult.Error("File size mismatch");

                    // Validate WAVE format
                    string waveFormat = Encoding.ASCII.GetString(reader.ReadBytes(4));
                    if (waveFormat != "WAVE")
                        return ValidationResult.Error("Invalid WAVE format");

                    // Find and validate fmt chunk
                    while (true)
                    {
                        string chunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                        int chunkSize = reader.ReadInt32();

                        if (chunkId == "fmt ")
                        {
                            if (chunkSize < 16)
                                return ValidationResult.Error("Invalid fmt chunk size");

                            var metadata = new WavMetadata
                            {
                                FileSize = fileSize,
                                AudioFormat = reader.ReadInt16() == 1 ? "PCM" : "Compressed",
                                Channels = reader.ReadInt16(),
                                SampleRate = reader.ReadInt32()
                            };

                            // Skip byte rate and block align
                            reader.BaseStream.Position += 6;

                            metadata.BitsPerSample = reader.ReadInt16();

                            // Calculate duration
                            long dataSize = stream.Length - 44; // Approximate data size
                            var bytesPerSample = metadata.BitsPerSample / 8;
                            var duration = TimeSpan.FromSeconds(
                                (double)dataSize / (metadata.SampleRate * metadata.Channels * bytesPerSample)
                            );

                            return ValidationResult.Success(duration, metadata);
                        }

                        // Skip this chunk if it's not fmt
                        if (reader.BaseStream.Position + chunkSize >= stream.Length)
                            return ValidationResult.Error("Could not find fmt chunk");

                        reader.BaseStream.Position += chunkSize;
                    }
                }
            }
            catch (Exception ex)
            {
                return ValidationResult.Error($"Error validating WAV stream: {ex.Message}");
            }
            finally
            {
                // Restore the stream's original position if needed
                if (stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }
            }
        }
    }
}
