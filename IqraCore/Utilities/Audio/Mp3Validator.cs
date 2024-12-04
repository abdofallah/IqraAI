namespace IqraCore.Utilities.Audio
{
    public class Mp3Validator
    {
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; }
            public Mp3Metadata Metadata { get; set; }

            public static ValidationResult Success(Mp3Metadata metadata) =>
                new ValidationResult { IsValid = true, Metadata = metadata };

            public static ValidationResult Error(string message) =>
                new ValidationResult { IsValid = false, ErrorMessage = message };
        }

        public class Mp3Metadata
        {
            public bool HasId3Tag { get; set; }
            public bool HasValidFrameHeader { get; set; }
            public long FileSize { get; set; }
            public string HeaderType { get; set; }
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
                return ValidationResult.Error($"Error opening MP3 file: {ex.Message}");
            }
        }

        public ValidationResult ValidateStream(Stream stream)
        {
            if (stream == null)
                return ValidationResult.Error("Stream is null");

            if (!stream.CanRead)
                return ValidationResult.Error("Stream is not readable");

            // Store original position
            long originalPosition = stream.Position;

            try
            {
                // Check minimum size for MP3 (at least 5 bytes for header)
                if (stream.Length < 5)
                    return ValidationResult.Error("File too small to be a valid MP3");

                var buffer = new byte[5];
                int bytesRead = stream.Read(buffer, 0, 5);

                if (bytesRead < 5)
                    return ValidationResult.Error("Couldn't read enough bytes for header validation");

                var metadata = new Mp3Metadata
                {
                    FileSize = stream.Length
                };

                // Check for ID3v2 header
                bool isId3Header = buffer[0] == 0x49 && // I
                                  buffer[1] == 0x44 && // D
                                  buffer[2] == 0x33;   // 3

                // Check for valid MP3 frame header
                bool isMp3Frame = buffer[0] == 0xFF &&
                                 (buffer[1] == 0xFB || buffer[1] == 0xFA || buffer[1] == 0xF3 || buffer[1] == 0xF2);

                metadata.HasId3Tag = isId3Header;
                metadata.HasValidFrameHeader = isMp3Frame;
                metadata.HeaderType = isId3Header ? "ID3v2" : isMp3Frame ? "MP3 Frame" : "Unknown";

                if (!isId3Header && !isMp3Frame)
                {
                    return ValidationResult.Error("Invalid MP3 file: No valid ID3 tag or MP3 frame header found");
                }

                // If we have an ID3 header, try to validate the first MP3 frame after the ID3 tag
                if (isId3Header)
                {
                    try
                    {
                        // Read ID3 size (next 4 bytes after "ID3")
                        stream.Position = originalPosition + 6;
                        var sizeBytes = new byte[4];
                        stream.Read(sizeBytes, 0, 4);

                        // ID3 size is stored as synchsafe integer
                        int id3Size = (sizeBytes[0] & 0x7f) << 21 |
                                    (sizeBytes[1] & 0x7f) << 14 |
                                    (sizeBytes[2] & 0x7f) << 7 |
                                    sizeBytes[3] & 0x7f;

                        // Check if we can find a valid frame header after ID3
                        stream.Position = originalPosition + id3Size + 10; // 10 is ID3 header size
                        if (stream.Position < stream.Length - 2)
                        {
                            var frameBuffer = new byte[2];
                            stream.Read(frameBuffer, 0, 2);
                            if (frameBuffer[0] != 0xFF || (frameBuffer[1] & 0xE0) != 0xE0)
                            {
                                metadata.HasValidFrameHeader = false;
                                return ValidationResult.Error("Invalid MP3 frame header after ID3 tag");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return ValidationResult.Error($"Error validating MP3 frame structure: {ex.Message}");
                    }
                }

                return ValidationResult.Success(metadata);
            }
            catch (Exception ex)
            {
                return ValidationResult.Error($"Error validating MP3 stream: {ex.Message}");
            }
            finally
            {
                // Restore the stream's original position
                if (stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }
            }
        }
    }
}
