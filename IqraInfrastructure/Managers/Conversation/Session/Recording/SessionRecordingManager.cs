using IqraCore.Entities.Helper.Audio;
using IqraInfrastructure.Helpers.Audio;
using IqraInfrastructure.Repositories.Conversation;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text;

namespace IqraInfrastructure.Managers.Conversation.Session.Recording
{
    public class SessionRecordingManager : IDisposable
    {
        private readonly string _sessionId;
        private readonly BusinessConversationAudioRepository _audioRepository;
        private readonly ILogger _logger;

        private readonly string _tempDirectory;
        private readonly ConcurrentDictionary<string, RecordingStreamContext> _activeStreams = new();
        private readonly object _lock = new();
        private bool _isFinalizing = false;

        private const int FileSampleRate = 16000;
        private const int FileBitsPerSample = 16;

        public SessionRecordingManager(string sessionId, BusinessConversationAudioRepository audioRepository, ILogger logger)
        {
            _sessionId = sessionId;
            _audioRepository = audioRepository;
            _logger = logger;

            // Create a temp folder for this specific session
            _tempDirectory = Path.Combine(Path.GetTempPath(), "IqraSessions", _sessionId);
            if (!Directory.Exists(_tempDirectory))
            {
                Directory.CreateDirectory(_tempDirectory);
            }
        }

        /// <summary>
        /// Writes a chunk of audio to the specific source's file stream.
        /// This is called synchronously by the Mixer Tick (fast IO).
        /// </summary>
        public void WriteAudioFrame(string sourceId, byte[] pcmData, int sampleRate, int bitsPerSample)
        {
            if (_isFinalizing || pcmData.Length == 0) return;

            byte[] writeData = pcmData;
            if (sampleRate != FileSampleRate || bitsPerSample != FileBitsPerSample)
            {
                (writeData, _) = AudioConversationHelper.Convert(
                    pcmData,
                    new() { 
                        Encoding = AudioEncodingTypeEnum.PCM,
                        SampleRateHz = sampleRate,
                        BitsPerSample = bitsPerSample
                    },
                    new () { 
                        RequestedEncoding = AudioEncodingTypeEnum.PCM,
                        RequestedSampleRateHz = FileSampleRate,
                        RequestedBitsPerSample = FileBitsPerSample
                    },
                    false
                );
            }

            // Get or Create the stream context for this source
            if (!_activeStreams.TryGetValue(sourceId, out var context))
            {
                lock (_lock)
                {
                    if (!_activeStreams.TryGetValue(sourceId, out context))
                    {
                        string filePath = Path.Combine(_tempDirectory, $"{sourceId}.raw");
                        context = new RecordingStreamContext
                        {
                            FileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read),
                            FilePath = filePath
                        };
                        _activeStreams[sourceId] = context;
                    }
                }
            }

            // Write raw bytes
            context.FileStream.Write(writeData, 0, writeData.Length);
        }

        /// <summary>
        /// Closes streams, converts to WAV, uploads to S3, and cleans up.
        /// </summary>
        public async Task FinalizeAndUploadAsync()
        {
            _isFinalizing = true;
            _logger.LogInformation("Finalizing recording for session {SessionId}...", _sessionId);

            var uploadTasks = new List<Task<(string, bool)>>();

            foreach (var kvp in _activeStreams)
            {
                string sourceId = kvp.Key;
                RecordingStreamContext context = kvp.Value;

                // 1. Close Stream to flush to disk
                context.FileStream.Close();
                await context.FileStream.DisposeAsync();

                // 2. Process and Upload
                uploadTasks.Add(Task.Run(async () =>
                {
                    return (sourceId, await ProcessSingleStreamAsync(sourceId, context));
                }));
            }

            await Task.WhenAll(uploadTasks);

            // 3. Cleanup Directory
            try
            {
                if (uploadTasks.Any(t => !t.Result.Item2))
                {
                    _logger.LogWarning("Recording finalization failed for session {SessionId}. Skipping deleting temp folder.", _sessionId);
                }
                else
                {
                    if (Directory.Exists(_tempDirectory))
                    {
                        Directory.Delete(_tempDirectory, true);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to delete temp recording directory: {Message}", ex.Message);
            }

            _logger.LogInformation("Recording finalization complete for session {SessionId}.", _sessionId);
        }

        private async Task<bool> ProcessSingleStreamAsync(string sourceId, RecordingStreamContext context)
        {
            string finalWavPath = context.FilePath.Replace(".raw", ".wav");

            try
            {
                // 1. Convert RAW PCM -> WAV (Add Header)
                // We do this locally to avoid loading the whole file into memory if it's huge.
                AddWavHeader(context.FilePath, finalWavPath, FileSampleRate, FileBitsPerSample, 1);

                // 2. Read the WAV file
                byte[] wavBytes = await File.ReadAllBytesAsync(finalWavPath);

                // 3. Upload to S3
                // Naming convention: {SessionId}/{SourceId}.wav
                // If SourceId is "Master", it's the full mix.
                string s3Reference = $"{_sessionId}/recordings/{sourceId}.wav";

                var result = await _audioRepository.StoreAudioAsync(s3Reference, wavBytes);
                if (!result)
                {
                    _logger.LogError("Failed to upload recording for source {Source}", sourceId);
                    return false;
                }

                _logger.LogDebug("Uploaded recording for {Source} ({Size} bytes)", sourceId, wavBytes.Length);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process/upload recording for source {Source}", sourceId);
                return false;
            }
        }

        public void Dispose()
        {
            _isFinalizing = true;
            foreach (var context in _activeStreams.Values)
            {
                context.FileStream?.Dispose();
            }
        }

        // Helper: WAV Header Generation
        private void AddWavHeader(string rawFilePath, string wavFilePath, int sampleRate, int bitsPerSample, int channels)
        {
            using (var rawStream = new FileStream(rawFilePath, FileMode.Open, FileAccess.Read))
            using (var wavStream = new FileStream(wavFilePath, FileMode.Create, FileAccess.Write))
            {
                long dataLength = rawStream.Length;

                // Write Header
                WriteWavHeader(wavStream, dataLength, sampleRate, bitsPerSample, channels);

                // Copy Data
                rawStream.CopyTo(wavStream);
            }
        }
        private void WriteWavHeader(Stream stream, long dataLength, int sampleRate, int bitsPerSample, int channels)
        {
            int blockAlign = channels * (bitsPerSample / 8);
            int averageBytesPerSecond = sampleRate * blockAlign;
            long riffSize = dataLength + 36;

            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write((int)riffSize);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16); // Subchunk1Size (16 for PCM)
                writer.Write((short)1); // AudioFormat (1 for PCM)
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(averageBytesPerSecond);
                writer.Write((short)blockAlign);
                writer.Write((short)bitsPerSample);
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write((int)dataLength);
            }
        }

        // Inner State Class
        private class RecordingStreamContext
        {
            public FileStream FileStream { get; set; } = null!;
            public string FilePath { get; set; } = string.Empty;
        }
    }
}