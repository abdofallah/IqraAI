using IqraCore.Entities.Helper.Audio;
using IqraInfrastructure.Helpers.Audio;
using IqraInfrastructure.Managers.Conversation.Session.Agent.AI.Helpers;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace IqraInfrastructure.Managers.Conversation.Session.Mixer
{
    public class SessionAudioMixer : IDisposable
    {
        private readonly string _sessionId;
        private readonly ILogger _logger;

        // Configuration
        private AudioFormatDetails _masterFormat;
        public static int FRAME_DURATION_MS = 20;
        private int _bytesPerFrame;

        // Jitter settings
        public static int MAX_BUFFER_MS = 120;

        // State
        private readonly ConcurrentDictionary<string, MixerInputChannel> _inputs = new();
        private readonly PeriodicTimer _timer;
        private readonly CancellationTokenSource _cts = new();
        private Task? _loopTask;
        private readonly SemaphoreSlim _semaphore = new(1);

        // Events
        public event Action<string, byte[], int, int, int>? AudioMixed;
        public event Action<string, byte[], int, int>? AudioFrameReadyForRecording;

        public SessionAudioMixer(string sessionId, int initialSampleRate, int initialBits, ILogger logger)
        {
            _sessionId = sessionId;
            _logger = logger;
            _masterFormat = new AudioFormatDetails()
            {
                SampleRate = initialSampleRate,
                BitsPerSample = initialBits
            };

            RecalculateBytesSizePerFrame();
            _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(FRAME_DURATION_MS));
        }

        public void Start()
        {
            if (_loopTask != null) return;
            _loopTask = Task.Run(MixingLoopAsync, _cts.Token);
            _logger.LogInformation("SessionAudioMixer started for session {SessionId} @ {Rate}Hz.", _sessionId, _masterFormat.SampleRate);
        }

        private void RecalculateBytesSizePerFrame()
        {
            int bytesPerSample = _masterFormat.BitsPerSample / 8;
            _bytesPerFrame = (_masterFormat.SampleRate * bytesPerSample * FRAME_DURATION_MS) / 1000;
        }

        public void UpdateMasterFormat(int sampleRate, int bits)
        {
            try
            {
                _semaphore.Wait(_cts.Token);

                var oldSampleRate = _masterFormat.SampleRate;
                var oldBitsPerSample = _masterFormat.BitsPerSample;

                _masterFormat.SampleRate = sampleRate;
                _masterFormat.BitsPerSample = bits;
                RecalculateBytesSizePerFrame();

                foreach (var channel in _inputs.Values)
                {
                    var channelOldRate = channel.CurrentFormat?.SampleRate ?? oldSampleRate;
                    var channelOldBits = channel.CurrentFormat?.BitsPerSample ?? oldBitsPerSample;

                    // Convert ALL queued chunks to the new format
                    var oldChunks = new List<TrackedAudioChunk>();
                    while (channel.SpeechQueue.TryDequeue(out var chunk))
                    {
                        oldChunks.Add(chunk);
                    }
                    channel.ResetBytesTracker();

                    foreach (var chunk in oldChunks)
                    {
                        try
                        {
                            var (converted, _) = AudioConversationHelper.Convert(
                                chunk.Data,
                                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = channelOldRate, BitsPerSample = channelOldBits },
                                new() { RequestedEncoding = AudioEncodingTypeEnum.PCM, RequestedSampleRateHz = sampleRate, RequestedBitsPerSample = bits },
                                false
                            );
                            channel.SpeechQueue.Enqueue(new TrackedAudioChunk { Data = converted, ArrivedAt = chunk.ArrivedAt });
                            channel.AddBytes(converted.Length);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "SessionAudioMixer: Failed to convert queued chunk during format upgrade");
                        }
                    }

                    // Convert leftover buffer if non-empty
                    if (channel.LeftoverBuffer.Length > 0)
                    {
                        try
                        {
                            var (convertedLeftover, _) = AudioConversationHelper.Convert(
                                channel.LeftoverBuffer,
                                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = channelOldRate, BitsPerSample = channelOldBits },
                                new() { RequestedEncoding = AudioEncodingTypeEnum.PCM, RequestedSampleRateHz = sampleRate, RequestedBitsPerSample = bits },
                                false
                            );
                            channel.LeftoverBuffer = convertedLeftover;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "SessionAudioMixer: Failed to convert leftover buffer during format upgrade");
                            channel.LeftoverBuffer = Array.Empty<byte>();
                        }
                    }

                    channel.CurrentFormat = new AudioFormatDetails { SampleRate = sampleRate, BitsPerSample = bits };
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public int GetBytesPerFrame() => _bytesPerFrame;

        public void RegisterMixer(string sourceId, MixerSourceType sourceType, int sampleRate, int bitsPerSample)
        {
            try
            {
                _semaphore.Wait(_cts.Token);

                _inputs.GetOrAdd(sourceId, _ => new MixerInputChannel()
                {
                    SourceType = sourceType,
                    CurrentFormat = new AudioFormatDetails
                    {
                        SampleRate = sampleRate,
                        BitsPerSample = bitsPerSample
                    }
                });
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void EnqueueInput(string sourceId, byte[] audioData, int sampleRate, int bitsPerSample)
        {
            try
            {
                _semaphore.Wait(_cts.Token);

                // Do not auto-create! If it's not registered, we drop.
                if (!_inputs.TryGetValue(sourceId, out var channel)) return;

                byte[] masterAudioData = AudioMixingHelper.ResampleIfFormatMismatch(
                    audioData,
                    new AudioFormatDetails() { SampleRate = sampleRate, BitsPerSample = bitsPerSample },
                    _masterFormat
                );

                channel.SpeechQueue.Enqueue(new TrackedAudioChunk { Data = masterAudioData, ArrivedAt = DateTime.UtcNow });
                channel.AddBytes(masterAudioData.Length);

                // --- JITTER BUFFER OVERFLOW PROTECTION (CLIENTS ONLY) ---
                // Agent overflow is handled by backpressure in the producer loop.
                // For clients, drop oldest frames to stay within real-time bounds.
                if (channel.SourceType == MixerSourceType.Client)
                {
                    double currentBufferMs = channel.GetExactDurationMs(_masterFormat.SampleRate, _masterFormat.BitsPerSample);

                    while (currentBufferMs > MAX_BUFFER_MS && !channel.SpeechQueue.IsEmpty)
                    {
                        if (channel.SpeechQueue.TryDequeue(out var dropped))
                        {
                            channel.RemoveBytes(dropped.Data.Length);
                            currentBufferMs = channel.GetExactDurationMs(_masterFormat.SampleRate, _masterFormat.BitsPerSample);
                        }
                        else break;
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void SetBackgroundSource(string sourceId, ConversationAIAgentBackgroundAudioProvider? provider)
        {
            if (_inputs.TryGetValue(sourceId, out var channel))
            {
                channel.BackgroundProvider = provider;
            }
        }

        public void ClearInputQueue(string sourceId)
        {
            try
            {
                _semaphore.Wait(_cts.Token);

                if (_inputs.TryGetValue(sourceId, out var channel))
                {
                    channel.SpeechQueue.Clear();
                    channel.LeftoverBuffer = Array.Empty<byte>();
                    channel.ResetBytesTracker(); // MUST reset byte tracker
                }
            }
            catch (OperationCanceledException) { /* Expected during shutdown */ }
            finally
            {
                _semaphore.Release();
            }
        }

        public double GetQueueDurationMs(string sourceId)
        {
            if (_inputs.TryGetValue(sourceId, out var channel))
            {
                return channel.GetExactDurationMs(_masterFormat.SampleRate, _masterFormat.BitsPerSample);
            }
            return 0;
        }

        private async Task MixingLoopAsync()
        {
            try
            {
                while (await _timer.WaitForNextTickAsync(_cts.Token))
                {
                    _semaphore.Wait(_cts.Token);
                    try
                    {
                        ProcessTick();
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in Mixer Loop for session {SessionId}", _sessionId);
            }
        }

        private void ProcessTick()
        {
            var channelProcessedAudio = new Dictionary<string, byte[]>();

            foreach (var kvp in _inputs)
            {
                string sourceId = kvp.Key;
                MixerInputChannel channel = kvp.Value;

                byte[] rawSpeech = GetNextSpeechFrame(sourceId, channel, _bytesPerFrame);
                byte[] masterMusic = Array.Empty<byte>();

                if (channel.BackgroundProvider != null)
                {
                    masterMusic = channel.BackgroundProvider.GetNextFrame(_bytesPerFrame);
                    if (masterMusic.Length != _bytesPerFrame)
                    {
                        masterMusic = AudioMixingHelper.ResampleIfFormatMismatch(masterMusic, channel.BackgroundProvider.Format, _masterFormat);
                    }
                }

                byte[] channelMix = AudioMixingHelper.MixMultiple(new List<byte[]> { rawSpeech, masterMusic }, _masterFormat.BitsPerSample);
                channelProcessedAudio[sourceId] = channelMix;

                AudioFrameReadyForRecording?.Invoke(sourceId, channelMix, _masterFormat.SampleRate, _masterFormat.BitsPerSample);
            }

            byte[] masterMix = AudioMixingHelper.MixMultiple(channelProcessedAudio.Values.ToList(), _masterFormat.BitsPerSample);
            AudioFrameReadyForRecording?.Invoke("Master", masterMix, _masterFormat.SampleRate, _masterFormat.BitsPerSample);

            foreach (var kvp in channelProcessedAudio)
            {
                string targetId = kvp.Key;
                byte[] sourceAudio = kvp.Value;
                byte[] mixMinus = AudioMixingHelper.Subtract(masterMix, sourceAudio, _masterFormat.BitsPerSample);

                AudioMixed?.Invoke(targetId, mixMinus, _masterFormat.SampleRate, _masterFormat.BitsPerSample, FRAME_DURATION_MS);
            }
        }

        private byte[] GetNextSpeechFrame(string sourceId, MixerInputChannel channel, int bytesNeeded)
        {
            byte[] result = new byte[bytesNeeded];
            int destOffset = 0;
            int actualBytesPulled = 0; // Track exactly how many actual bytes we consumed

            if (channel.LeftoverBuffer.Length > 0)
            {
                int toCopy = Math.Min(channel.LeftoverBuffer.Length, bytesNeeded);
                Array.Copy(channel.LeftoverBuffer, 0, result, 0, toCopy);
                destOffset += toCopy;
                actualBytesPulled += toCopy;

                if (toCopy < channel.LeftoverBuffer.Length)
                {
                    int remaining = channel.LeftoverBuffer.Length - toCopy;
                    byte[] newLeftover = new byte[remaining];
                    Array.Copy(channel.LeftoverBuffer, toCopy, newLeftover, 0, remaining);
                    channel.LeftoverBuffer = newLeftover;
                }
                else
                {
                    channel.LeftoverBuffer = Array.Empty<byte>();
                }
            }

            while (destOffset < bytesNeeded)
            {
                if (channel.SpeechQueue.TryDequeue(out var trackedChunk))
                {
                    var chunk = trackedChunk.Data;
                    int spaceRemaining = bytesNeeded - destOffset;

                    if (chunk.Length <= spaceRemaining)
                    {
                        Array.Copy(chunk, 0, result, destOffset, chunk.Length);
                        destOffset += chunk.Length;
                        actualBytesPulled += chunk.Length;
                    }
                    else
                    {
                        Array.Copy(chunk, 0, result, destOffset, spaceRemaining);
                        actualBytesPulled += spaceRemaining;

                        int leftoverSize = chunk.Length - spaceRemaining;
                        channel.LeftoverBuffer = new byte[leftoverSize];
                        Array.Copy(chunk, spaceRemaining, channel.LeftoverBuffer, 0, leftoverSize);

                        destOffset += spaceRemaining;
                    }
                }
                else
                {
                    break;
                }
            }

            // Exactly deduct the actual bytes we pulled from tracking
            if (actualBytesPulled > 0)
            {
                channel.RemoveBytes(actualBytesPulled);
            }

            return result;
        }

        public void Dispose()
        {
            _cts.Cancel();
            _timer.Dispose();
            _inputs.Clear();
        }

        public enum MixerSourceType
        {
            Client,
            Agent
        }

        private class TrackedAudioChunk
        {
            public byte[] Data { get; set; } = Array.Empty<byte>();
            public DateTime ArrivedAt { get; set; }
        }

        private class MixerInputChannel
        {
            public MixerSourceType SourceType { get; set; }
            public ConcurrentQueue<TrackedAudioChunk> SpeechQueue { get; } = new();
            public byte[] LeftoverBuffer { get; set; } = Array.Empty<byte>();
            public ConversationAIAgentBackgroundAudioProvider? BackgroundProvider { get; set; }
            public AudioFormatDetails? CurrentFormat { get; set; }

            private long _totalBytesBuffered = 0;

            public void AddBytes(int count) => Interlocked.Add(ref _totalBytesBuffered, count);
            public void RemoveBytes(int count) => Interlocked.Add(ref _totalBytesBuffered, -count);
            public void ResetBytesTracker() => Interlocked.Exchange(ref _totalBytesBuffered, 0);
            public long GetTotalBytesBuffered() => Interlocked.Read(ref _totalBytesBuffered);

            public double GetExactDurationMs(int sampleRate, int bitsPerSample)
            {
                long bytes = Interlocked.Read(ref _totalBytesBuffered);
                if (bytes < 0) bytes = 0; // Clamp: can drift negative during concurrent clear/pull overlap
                int bytesPerSecond = sampleRate * (bitsPerSample / 8);
                if (bytesPerSecond == 0) return 0;
                return (double)bytes / bytesPerSecond * 1000.0;
            }
        }
    }
}