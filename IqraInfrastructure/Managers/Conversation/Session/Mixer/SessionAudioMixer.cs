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
        public static int MAX_BUFFER_MS = 80;

        // State
        private readonly ConcurrentDictionary<string, MixerInputChannel> _inputs = new();
        private readonly PeriodicTimer _timer;
        private readonly CancellationTokenSource _cts = new();
        private Task? _loopTask;
        private readonly object _lock = new();
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
            lock (_lock)
            {
                try
                {
                    _semaphore.Wait(_cts.Token);

                    _masterFormat.SampleRate = sampleRate;
                    _masterFormat.BitsPerSample = bits;
                    RecalculateBytesSizePerFrame();

                    foreach (var channel in _inputs.Values)
                    {
                        // Reset exact byte counter since we are about to modify the buffer sizes
                        long preConvertBytes = channel.GetTotalBytesBuffered();

                        if (!channel.SpeechQueue.IsEmpty && channel.SpeechQueue.TryDequeue(out var buffer))
                        {
                            try
                            {
                                var (convertedBuffer, _) = AudioConversationHelper.Convert(
                                    buffer.Data,
                                    new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = channel.CurrentFormat!.SampleRate, BitsPerSample = channel.CurrentFormat.BitsPerSample },
                                    new() { RequestedEncoding = AudioEncodingTypeEnum.PCM, RequestedSampleRateHz = sampleRate, RequestedBitsPerSample = bits },
                                    false
                                );

                                channel.SpeechQueue.Enqueue(new TrackedAudioChunk { Data = convertedBuffer, ArrivedAt = buffer.ArrivedAt });

                                // Recalculate bytes
                                channel.RemoveBytes(buffer.Data.Length);
                                channel.AddBytes(convertedBuffer.Length);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "SessionAudioMixer: Failed to convert buffer to new format");
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
                channel.AddBytes(masterAudioData.Length); // Accurately track bytes added

                // --- SMART TIERED JITTER BUFFER (ONLY FOR CLIENTS) ---
                if (channel.SourceType == MixerSourceType.Client)
                {
                    double currentBufferMs = channel.GetExactDurationMs(_masterFormat.SampleRate, _masterFormat.BitsPerSample);

                    while (currentBufferMs > MAX_BUFFER_MS)
                    {
                        ApplySmartJitterReduction(channel);
                        // Re-evaluate duration after reduction
                        currentBufferMs = channel.GetExactDurationMs(_masterFormat.SampleRate, _masterFormat.BitsPerSample);

                        // Failsafe break if queue is somehow empty but bytes remain
                        if (channel.SpeechQueue.IsEmpty) break;
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void ApplySmartJitterReduction(MixerInputChannel channel)
        {
            if (channel.SpeechQueue.TryDequeue(out var chunk1))
            {
                channel.RemoveBytes(chunk1.Data.Length);

                // TIER 1: Silence Dropping
                bool isSilence = AudioDSPHelper.IsSilence(chunk1.Data, _masterFormat.BitsPerSample);
                if (isSilence)
                {
                    return; // Dropped silence, done.
                }

                // TIER 2: Time-Scale Shrinking (Overlap-Add)
                if (channel.SpeechQueue.TryDequeue(out var chunk2))
                {
                    channel.RemoveBytes(chunk2.Data.Length);

                    // Crossfade the two chunks to half their total size
                    byte[] shrunkData = AudioDSPHelper.ShrinkAudio(chunk1.Data, chunk2.Data, _masterFormat.BitsPerSample);

                    // Prepend the shrunk data onto the LeftoverBuffer so it's consumed immediately, maintaining order.
                    byte[] newLeftover = new byte[channel.LeftoverBuffer.Length + shrunkData.Length];
                    Buffer.BlockCopy(channel.LeftoverBuffer, 0, newLeftover, 0, channel.LeftoverBuffer.Length);
                    Buffer.BlockCopy(shrunkData, 0, newLeftover, channel.LeftoverBuffer.Length, shrunkData.Length);

                    channel.LeftoverBuffer = newLeftover;
                    channel.AddBytes(shrunkData.Length); // Add back the newly sized bytes
                }
                else
                {
                    // Fallback if no second chunk is available to crossfade
                    byte[] newLeftover = new byte[channel.LeftoverBuffer.Length + chunk1.Data.Length];
                    Buffer.BlockCopy(channel.LeftoverBuffer, 0, newLeftover, 0, channel.LeftoverBuffer.Length);
                    Buffer.BlockCopy(chunk1.Data, 0, newLeftover, channel.LeftoverBuffer.Length, chunk1.Data.Length);

                    channel.LeftoverBuffer = newLeftover;
                    channel.AddBytes(chunk1.Data.Length);
                }
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
            if (_inputs.TryGetValue(sourceId, out var channel))
            {
                channel.SpeechQueue.Clear();
                channel.LeftoverBuffer = Array.Empty<byte>();
                channel.ResetBytesTracker(); // MUST reset byte tracker
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
                    lock (_lock)
                    {
                        ProcessTick();
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
                int bytesPerSecond = sampleRate * (bitsPerSample / 8);
                if (bytesPerSecond == 0) return 0;
                return (double)bytes / bytesPerSecond * 1000.0;
            }
        }
    }
}