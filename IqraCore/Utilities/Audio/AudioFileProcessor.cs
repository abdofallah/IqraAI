using Microsoft.AspNetCore.Http;
using System.Data.HashFunction.xxHash;

namespace IqraCore.Utilities.Audio
{
    public class AudioFileProcessor
    {
        private readonly WavValidator _wavValidator;
        private readonly Mp3Validator _mp3Validator;
        private readonly IxxHash _xxHash;

        public class AudioValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; }
            public string FileType { get; set; }
            public TimeSpan? Duration { get; set; }
            public string Hash { get; set; }
            public ReadOnlyMemory<byte> FileBytes { get; set; }
            public string ContentType => FileType?.ToLowerInvariant() switch
            {
                "wav" => "audio/wav",
                "mp3" => "audio/mpeg",
                _ => "application/octet-stream"
            };
        }

        public AudioFileProcessor()
        {
            _wavValidator = new WavValidator();
            _mp3Validator = new Mp3Validator();
            _xxHash = xxHashFactory.Instance.Create(new xxHashConfig { HashSizeInBits = 64 });
        }

        public async Task<AudioValidationResult> ValidateAudioFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return new AudioValidationResult { IsValid = false, ErrorMessage = "File is empty" };

            // Check file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".wav" && extension != ".mp3")
                return new AudioValidationResult { IsValid = false, ErrorMessage = "Unsupported file format. Only WAV and MP3 files are allowed." };

            // Read file into memory once
            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            // Calculate hash from the complete file
            var hashValue = _xxHash.ComputeHash(fileBytes);
            string hash = BitConverter.ToString(hashValue.Hash).Replace("-", "").ToLowerInvariant();

            // Validate the file
            using (var validationStream = new MemoryStream(fileBytes))
            {
                if (extension == ".wav")
                {
                    var result = _wavValidator.ValidateStream(validationStream);
                    return new AudioValidationResult
                    {
                        IsValid = result.IsValid,
                        ErrorMessage = result.ErrorMessage,
                        FileType = "wav",
                        Duration = result.Duration,
                        Hash = hash,
                        FileBytes = fileBytes
                    };
                }
                else // mp3
                {
                    var result = _mp3Validator.ValidateStream(validationStream);
                    return new AudioValidationResult
                    {
                        IsValid = result.IsValid,
                        ErrorMessage = result.ErrorMessage,
                        FileType = "mp3",
                        Duration = null,
                        Hash = hash,
                        FileBytes = fileBytes
                    };
                }
            }
        }
    }
}
