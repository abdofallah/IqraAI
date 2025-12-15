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
        private const int FRAME_DURATION_MS = 30;
        private int _bytesPerFrame;

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

            RecalculateFrameSize();

            _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(FRAME_DURATION_MS));
        }

        public void Start()
        {
            if (_loopTask != null) return;
            _loopTask = Task.Run(MixingLoopAsync, _cts.Token);
            _logger.LogInformation("SessionAudioMixer started for session {SessionId} @ {Rate}Hz.", _sessionId, _masterFormat.SampleRate);
        }

        // Configuration
        private void RecalculateFrameSize()
        {
            int bytesPerSample = _masterFormat.BitsPerSample / 8;
            _bytesPerFrame = (_masterFormat.SampleRate * bytesPerSample * FRAME_DURATION_MS) / 1000;
        }
        public void UpdateMasterFormat(int sampleRate, int bits)
        {
            lock (_lock)
            {
                _semaphore.Wait(_cts.Token);

                _masterFormat.SampleRate = sampleRate;
                _masterFormat.BitsPerSample = bits;
                RecalculateFrameSize();

                // We must convert the existing buffer if any for all channels to the new format
                foreach (var channel in _inputs)
                {
                    if (!channel.Value.SpeechQueue.IsEmpty && channel.Value.SpeechQueue.TryDequeue(out var buffer))
                    {
                        try
                        {
                            var (convertedBuffer, _) = AudioConversationHelper.Convert(
                                buffer,
                                new()
                                {
                                    Encoding = AudioEncodingTypeEnum.PCM,
                                    SampleRateHz = channel.Value.CurrentFormat!.SampleRate,
                                    BitsPerSample = channel.Value.CurrentFormat.BitsPerSample
                                },
                                new()
                                {
                                    RequestedEncoding = AudioEncodingTypeEnum.PCM,
                                    RequestedSampleRateHz = channel.Value.CurrentFormat!.SampleRate,
                                    RequestedBitsPerSample = channel.Value.CurrentFormat.BitsPerSample
                                },
                                false
                            );

                            channel.Value.SpeechQueue.Enqueue(convertedBuffer);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "SessionAudioMixer: Failed to convert buffer to new format for {channel}", channel.Key);
                        }
                    }
                }

                _semaphore.Release();

                _logger.LogInformation("SessionAudioMixer: Master Format updated to {Rate}Hz {Bits}bit.", sampleRate, bits);
            }
        }

        // Input Management
        public void EnqueueInput(string sourceId, byte[] audioData, int sampleRate, int bitsPerSample)
        {
            _semaphore.Wait(_cts.Token);
            _semaphore.Release();

            var channel = _inputs.GetOrAdd(sourceId, _ => new MixerInputChannel()
            {
                CurrentFormat = new AudioFormatDetails
                {
                    SampleRate = sampleRate,
                    BitsPerSample = bitsPerSample
                }
            });

            byte[] masterAudioData = AudioMixingHelper.ResampleIfFormatMismatch(
                audioData,
                new AudioFormatDetails()
                {
                    SampleRate = sampleRate,
                    BitsPerSample = bitsPerSample
                },
                _masterFormat
            );

            channel.SpeechQueue.Enqueue(masterAudioData);
        }
        public void SetBackgroundSource(string sourceId, ConversationAIAgentBackgroundAudioProvider? provider)
        {
            var channel = _inputs.GetOrAdd(sourceId, _ => new MixerInputChannel()
            {
                CurrentFormat = new AudioFormatDetails
                {
                    SampleRate = _masterFormat.SampleRate,
                    BitsPerSample = _masterFormat.BitsPerSample
                }
            });
            channel.BackgroundProvider = provider;
        }
        public void ClearInputQueue(string sourceId)
        {
            if (_inputs.TryGetValue(sourceId, out var channel))
            {
                // Clear Speech only. Background music persists.
                channel.SpeechQueue.Clear();
                // Reset fragmentation buffer
                channel.LeftoverBuffer = Array.Empty<byte>();
            }
        }
        public double GetQueueDurationMs(string sourceId)
        {
            if (_inputs.TryGetValue(sourceId, out var channel))
            {
                // Estimation based on count * frame size. 
                // Since Enqueue pushes raw blocks, this is an estimate.
                return channel.SpeechQueue.Count * FRAME_DURATION_MS;
            }
            return 0;
        }

        // The Core Loop
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
                // todo kill the session??
            }
        }
        private void ProcessTick()
        {
            var channelProcessedAudio = new Dictionary<string, byte[]>();

            // 1. PROCESS INDIVIDUAL CHANNELS
            foreach (var kvp in _inputs)
            {
                string sourceId = kvp.Key;
                MixerInputChannel channel = kvp.Value;

                // A. Get Speech (20ms worth)
                // We need exactly _bytesPerFrame. 
                // Channel handles the buffering/fragmentation logic helper.
                byte[] rawSpeech = GetNextSpeechFrame(channel, _bytesPerFrame);

                // C. Get Background Music (if any)
                byte[] masterMusic = Array.Empty<byte>();
                if (channel.BackgroundProvider != null)
                {
                    // Provider returns data. We assume provider matches master or needs resample.
                    // For now, we assume Agent reloads provider on format change, so it matches.
                    // If not, we could resample here too.
                    masterMusic = channel.BackgroundProvider.GetNextFrame(_bytesPerFrame);

                    // Safety check on music length
                    if (masterMusic.Length != _bytesPerFrame)
                    {
                        masterMusic = AudioMixingHelper.ResampleIfFormatMismatch(
                            masterMusic,
                            channel.BackgroundProvider.Format,
                            _masterFormat
                        );
                    }
                }

                // D. Pre-Mix (Speech + Music) for this channel
                // This represents "What this participant is contributing to the room"
                byte[] channelMix = AudioMixingHelper.MixMultiple(new List<byte[]> { rawSpeech, masterMusic }, _masterFormat.BitsPerSample);

                channelProcessedAudio[sourceId] = channelMix;

                // E. Send to Recording (Individual Track)
                AudioFrameReadyForRecording?.Invoke(sourceId, channelMix, _masterFormat.SampleRate, _masterFormat.BitsPerSample);
            }

            // 2. MASTER MIX
            // Sum of all channel contributions
            byte[] masterMix = AudioMixingHelper.MixMultiple(channelProcessedAudio.Values.ToList(), _masterFormat.BitsPerSample);

            // 3. Send to Recording (Master Track)
            AudioFrameReadyForRecording?.Invoke("Master", masterMix, _masterFormat.SampleRate, _masterFormat.BitsPerSample);

            // 4. DISTRIBUTION (Mix-Minus)
            foreach (var kvp in channelProcessedAudio)
            {
                string targetId = kvp.Key;
                byte[] sourceAudio = kvp.Value;

                // Client hears: (Everyone - Themselves)
                byte[] mixMinus = AudioMixingHelper.Subtract(masterMix, sourceAudio, _masterFormat.BitsPerSample);

                // TODO DEBUGGING ONLY
                AudioFrameReadyForRecording?.Invoke($"Master-self-{targetId}", masterMix, _masterFormat.SampleRate, _masterFormat.BitsPerSample);

                AudioMixed?.Invoke(targetId, mixMinus, _masterFormat.SampleRate, _masterFormat.BitsPerSample, FRAME_DURATION_MS);
            }
        }
        /// <summary>
        /// Helper to pull exactly 'bytesNeeded' from the channel's queue, 
        /// handling cases where queued items are larger or smaller than a frame.
        /// </summary>
        private byte[] GetNextSpeechFrame(MixerInputChannel channel, int bytesNeeded)
        {
            byte[] result = new byte[bytesNeeded]; // Zero-filled (Silence)
            int destOffset = 0;

            // 1. Drain Leftover
            if (channel.LeftoverBuffer.Length > 0)
            {
                int toCopy = Math.Min(channel.LeftoverBuffer.Length, bytesNeeded);
                Array.Copy(channel.LeftoverBuffer, 0, result, 0, toCopy);
                destOffset += toCopy;

                // Shift Leftover or Clear
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

            // 2. Drain Queue
            while (destOffset < bytesNeeded)
            {
                if (channel.SpeechQueue.TryDequeue(out var chunk))
                {
                    int spaceRemaining = bytesNeeded - destOffset;

                    if (chunk.Length <= spaceRemaining)
                    {
                        // Fits entirely
                        Array.Copy(chunk, 0, result, destOffset, chunk.Length);
                        destOffset += chunk.Length;
                    }
                    else
                    {
                        // Chunk is too big, take what we need, save rest
                        Array.Copy(chunk, 0, result, destOffset, spaceRemaining);

                        int leftoverSize = chunk.Length - spaceRemaining;
                        channel.LeftoverBuffer = new byte[leftoverSize];
                        Array.Copy(chunk, spaceRemaining, channel.LeftoverBuffer, 0, leftoverSize);

                        destOffset += spaceRemaining;
                        // Loop ends because destOffset == bytesNeeded
                    }
                }
                else
                {
                    // Queue empty. 
                    // Result is already pre-filled with zeros from 'new byte[]'.
                    // Just break.
                    break;
                }
            }

            return result;
        }

        public void Dispose()
        {
            _cts.Cancel();
            _timer.Dispose();
            _inputs.Clear();
        }

        // Inner State Class
        private class MixerInputChannel
        {
            public ConcurrentQueue<byte[]> SpeechQueue { get; } = new();
            public byte[] LeftoverBuffer { get; set; } = Array.Empty<byte>();
            public ConversationAIAgentBackgroundAudioProvider? BackgroundProvider { get; set; }
            public AudioFormatDetails? CurrentFormat { get; set; }
        }
    }
}