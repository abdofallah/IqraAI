using IqraInfrastructure.Helpers.Audio;
using IqraInfrastructure.Managers.Conversation.Session.Agent.AI.Helpers; // For AudioFormatDetails, BackgroundProvider
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace IqraInfrastructure.Managers.Conversation.Session.Mixer
{
    public class SessionAudioMixer : IDisposable
    {
        private readonly string _sessionId;
        private readonly ILogger _logger;

        // Configuration
        private int _masterSampleRate;
        private int _masterBitsPerSample;
        private int _masterChannels = 1;
        private const int FRAME_DURATION_MS = 20; // The Heartbeat Speed
        private int _bytesPerFrame; // Calculated based on Master Format

        // State
        private readonly ConcurrentDictionary<string, MixerInputChannel> _inputs = new();
        private readonly PeriodicTimer _timer;
        private readonly CancellationTokenSource _cts = new();
        private Task? _loopTask;
        private readonly object _lock = new();

        // Events
        // TargetId, AudioData, MasterSampleRate, MasterBits
        public event Action<string, byte[], int, int>? AudioMixed;

        // Event for Recording Manager (Phase 5) -> SourceId (or "Master"), Data
        public event Action<string, byte[], int, int>? AudioFrameReadyForRecording;

        public SessionAudioMixer(string sessionId, int initialSampleRate, int initialBits, ILogger logger)
        {
            _sessionId = sessionId;
            _logger = logger;
            _masterSampleRate = initialSampleRate;
            _masterBitsPerSample = initialBits;

            RecalculateFrameSize();

            // Initialize Timer
            _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(FRAME_DURATION_MS));
        }

        public void Start()
        {
            if (_loopTask != null) return;
            _loopTask = Task.Run(MixingLoopAsync, _cts.Token);
            _logger.LogInformation("SessionAudioMixer started for session {SessionId} @ {Rate}Hz.", _sessionId, _masterSampleRate);
        }

        // --- Configuration ---

        private void RecalculateFrameSize()
        {
            int bytesPerSample = _masterBitsPerSample / 8;
            _bytesPerFrame = (_masterSampleRate * bytesPerSample * _masterChannels * FRAME_DURATION_MS) / 1000;
        }

        public void UpdateMasterFormat(int sampleRate, int bits)
        {
            lock (_lock)
            {
                _masterSampleRate = sampleRate;
                _masterBitsPerSample = bits;
                RecalculateFrameSize();
                _logger.LogInformation("SessionAudioMixer: Master Format updated to {Rate}Hz {Bits}bit.", sampleRate, bits);
            }
        }

        // --- Input Management ---

        public void EnqueueInput(string sourceId, byte[] audioData, int sampleRate, int bitsPerSample)
        {
            var channel = _inputs.GetOrAdd(sourceId, _ => new MixerInputChannel());

            // Update the channel's knowledge of its own format
            channel.CurrentFormat = new AudioFormatDetails
            {
                SampleRate = sampleRate,
                BitsPerSample = bitsPerSample
            };

            // Slice and Enqueue
            // Note: We assume the input 'audioData' is roughly chunked, but we must ensure we don't break frames.
            // For robustness, we just enqueue the raw block. The Dequeue logic handles consumption.
            // Ideally, the Agent/Client sends exactly 20ms chunks, but we handle variation.
            channel.SpeechQueue.Enqueue(audioData);
        }

        public void SetBackgroundSource(string sourceId, ConversationAIAgentBackgroundAudioProvider? provider)
        {
            var channel = _inputs.GetOrAdd(sourceId, _ => new MixerInputChannel());
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

        // --- The Core Loop ---

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
            // Snapshot current master format (in case it changes mid-tick, though we locked)
            var masterFormat = new AudioFormatDetails
            {
                SampleRate = _masterSampleRate,
                BitsPerSample = _masterBitsPerSample
            };

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

                // B. Resample Speech if needed
                // If the client sent 8k but master is 16k, convert now.
                byte[] masterSpeech = AudioMixingHelper.ResampleIfFormatMismatch(
                    rawSpeech,
                    channel.CurrentFormat ?? masterFormat, // Fallback if unknown
                    masterFormat
                );

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
                            masterFormat
                        );
                    }
                }

                // D. Pre-Mix (Speech + Music) for this channel
                // This represents "What this participant is contributing to the room"
                byte[] channelMix = AudioMixingHelper.MixMultiple(new List<byte[]> { masterSpeech, masterMusic }, _masterBitsPerSample);

                channelProcessedAudio[sourceId] = channelMix;

                // E. Send to Recording (Individual Track)
                AudioFrameReadyForRecording?.Invoke(sourceId, channelMix, masterFormat.SampleRate, masterFormat.BitsPerSample);
            }

            // 2. MASTER MIX
            // Sum of all channel contributions
            byte[] masterMix = AudioMixingHelper.MixMultiple(channelProcessedAudio.Values.ToList(), _masterBitsPerSample);

            // 3. Send to Recording (Master Track)
            AudioFrameReadyForRecording?.Invoke("Master", masterMix, masterFormat.SampleRate, masterFormat.BitsPerSample);

            // 4. DISTRIBUTION (Mix-Minus)
            foreach (var kvp in channelProcessedAudio)
            {
                string targetId = kvp.Key;
                byte[] sourceAudio = kvp.Value;

                // Client hears: (Everyone - Themselves)
                byte[] mixMinus = AudioMixingHelper.Subtract(masterMix, sourceAudio, _masterBitsPerSample);

                AudioMixed?.Invoke(targetId, mixMinus, _masterSampleRate, _masterBitsPerSample);
            }
        }

        /// <summary>
        /// Helper to pull exactly 'bytesNeeded' from the channel's queue, 
        /// handling cases where queued items are larger or smaller than a frame.
        /// </summary>
        private byte[] GetNextSpeechFrame(MixerInputChannel channel, int bytesNeeded)
        {
            // If we have a leftover buffer from previous large chunk, use it.
            // Logic simplified for clarity: We construct a buffer.

            var buffer = new MemoryStream();
            int bytesCollected = 0;

            // 1. Use Leftover
            if (channel.LeftoverBuffer.Length > 0)
            {
                int toTake = Math.Min(channel.LeftoverBuffer.Length, bytesNeeded);
                buffer.Write(channel.LeftoverBuffer, 0, toTake);
                bytesCollected += toTake;

                // Update leftover
                if (toTake < channel.LeftoverBuffer.Length)
                {
                    // Still have leftovers
                    byte[] newLeftover = new byte[channel.LeftoverBuffer.Length - toTake];
                    Array.Copy(channel.LeftoverBuffer, toTake, newLeftover, 0, newLeftover.Length);
                    channel.LeftoverBuffer = newLeftover;
                }
                else
                {
                    channel.LeftoverBuffer = Array.Empty<byte>();
                }
            }

            // 2. Pull from Queue if needed
            while (bytesCollected < bytesNeeded)
            {
                if (channel.SpeechQueue.TryDequeue(out var chunk))
                {
                    int needed = bytesNeeded - bytesCollected;
                    int toTake = Math.Min(chunk.Length, needed);

                    buffer.Write(chunk, 0, toTake);
                    bytesCollected += toTake;

                    if (toTake < chunk.Length)
                    {
                        // We took part of a chunk. Save the rest as leftover.
                        int remaining = chunk.Length - toTake;
                        channel.LeftoverBuffer = new byte[remaining];
                        Array.Copy(chunk, toTake, channel.LeftoverBuffer, 0, remaining);
                    }
                }
                else
                {
                    // Starvation / Silence
                    // Fill the rest with zeros
                    int needed = bytesNeeded - bytesCollected;
                    buffer.Write(new byte[needed], 0, needed); // Write zeros
                    break;
                }
            }

            return buffer.ToArray();
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