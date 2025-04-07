using IqraCore.Entities.Conversation.Events; // For ConversationAudioGeneratedEventArgs
using IqraCore.Utilities.Audio;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.TTS; // For TTSProviderManager
using IqraInfrastructure.Repositories.Business; // For BusinessAgentAudioRepository
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Runtime.InteropServices; // For MemoryMarshal


namespace IqraInfrastructure.Managers.Conversation.Agent.AI
{
    // Define SpeechSegment struct here or in a shared location
    internal readonly struct SpeechSegment
    {
        public ReadOnlyMemory<byte> AudioData { get; }
        public TimeSpan Duration { get; }
        public SpeechSegment(ReadOnlyMemory<byte> audioData, TimeSpan duration)
        {
            AudioData = audioData;
            Duration = duration;
        }
    }

    public class ConversationAIAgentAudioOutput : IDisposable
    {
        // Event for Orchestrator to send audio chunks out
        public event EventHandler<ConversationAudioGeneratedEventArgs>? AudioChunkGenerated;
        // Event to signal when the speech queue is empty and the last chunk is sent
        public event Action? SpeechPlaybackComplete;

        private readonly ILogger<ConversationAIAgentAudioOutput> _logger;
        private readonly ConversationAIAgentState _agentState;
        private readonly TTSProviderManager _ttsProviderManager;
        private readonly BusinessAgentAudioRepository _audioRepository;
        private readonly BusinessManager _businessManager; // For TTS Integration data


        // --- Audio Processing & Buffering Members ---
        private const int SampleRate = 16000; // Hz TODO: Make configurable?
        private const int BitsPerSample = 16; // bits
        private const int Channels = 1; // mono
        private const int BytesPerSample = BitsPerSample / 8;
        private const int ChunkDurationMs = 300; // Desired chunk duration
        private const int BytesPerChunk = SampleRate * BytesPerSample * Channels * ChunkDurationMs / 1000;

        // Queues & Tasks
        private readonly BlockingCollection<SpeechSegment> _speechAudioQueue = new(new ConcurrentQueue<SpeechSegment>());
        private Task? _audioSendingTask;
        private CancellationTokenSource _audioSendingCTS = new(); // CTS for the sending loop

        // Background Audio State
        private int _backgroundAudioPosition = 0;

        // Current Speech Playback State
        private ReadOnlyMemory<byte> _currentSpeechSegment = ReadOnlyMemory<byte>.Empty;
        private TimeSpan _currentSpeechDuration = TimeSpan.Zero;
        private int _currentSpeechPosition = 0;

        // Volume Fading State (managed here, affects _agentState.CurrentAgentVolumeFactor)
        private CancellationTokenSource? _volumeFadeCTS = null;
        private Task? _volumeFadeTask = null;

        // TTS Specific Task Management
        private CancellationTokenSource? _currentTtsTaskCTS = null;

        public ConversationAIAgentAudioOutput(
            ILoggerFactory loggerFactory,
            ConversationAIAgentState agentState,
            TTSProviderManager ttsProviderManager,
            BusinessAgentAudioRepository audioRepository,
            BusinessManager businessManager)
        {
            _logger = loggerFactory.CreateLogger<ConversationAIAgentAudioOutput>();
            _agentState = agentState;
            _ttsProviderManager = ttsProviderManager;
            _audioRepository = audioRepository;
            _businessManager = businessManager; // Store businessManager
        }

        public async Task InitializeAsync(CancellationToken agentCTS)
        {
            _audioSendingCTS = CancellationTokenSource.CreateLinkedTokenSource(agentCTS); // Link to agent shutdown

            // --- Move logic from InitalizeTTSForLangauge here ---
            await InitializeTTSAsync(); // Extracted TTS setup

            // --- Move logic from LoadBackgroundMusicAsync here ---
            await LoadBackgroundMusicAsync(); // Extracted background music loading

            // Start audio sending task
            _audioSendingTask = Task.Run(() => ProcessAudioSpeakingQueueAsync(_audioSendingCTS.Token), _audioSendingCTS.Token);
            _logger.LogInformation("AudioOutput module initialized for Agent {AgentId}.", _agentState.AgentId);
        }
        public async Task ReInitializeForLanguageAsync(CancellationToken agentCTS) // Pass token if needed
        {
            _logger.LogInformation("Agent {AgentId}: Re-initializing Audio Output Handler for new language.", _agentState.AgentId);
            await CancelCurrentSpeechPlaybackAsync(); // Stop current playback/TTS
            await InitializeTTSAsync(); // Re-initialize TTS service
                                        // Background music usually doesn't need reloading on language change
        }
        private async Task InitializeTTSAsync()
        {
            if (_agentState.BusinessAppAgent == null || string.IsNullOrEmpty(_agentState.CurrentLanguageCode) || _agentState.AgentConfiguration == null)
            {
                _logger.LogError("Agent {AgentId}: Cannot initialize TTS - missing required state.", _agentState.AgentId);
                throw new InvalidOperationException("TTS initialization requires BusinessAppAgent, LanguageCode, and AgentConfiguration.");
            }

            var defaultTTSServiceInfo = _agentState.BusinessAppAgent.Integrations.TTS[_agentState.CurrentLanguageCode][0];
            var ttsBusinessIntegrationDataResult = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(_agentState.AgentConfiguration.BusinessId, defaultTTSServiceInfo.Id);

            if (!ttsBusinessIntegrationDataResult.Success || ttsBusinessIntegrationDataResult.Data == null)
            {
                _logger.LogError("Agent {AgentId}: Business app TTS integration {IntegrationId} not found", _agentState.AgentId, defaultTTSServiceInfo.Id);
                throw new InvalidOperationException($"Business app TTS integration {defaultTTSServiceInfo.Id} not found");
            }
            _agentState.TTSBusinessIntegrationData = ttsBusinessIntegrationDataResult.Data;

            var ttsServiceResult = await _ttsProviderManager.BuildProviderServiceByIntegration(_agentState.TTSBusinessIntegrationData, defaultTTSServiceInfo, new Dictionary<string, string> { });
            if (!ttsServiceResult.Success || ttsServiceResult.Data == null)
            {
                _logger.LogError("Agent {AgentId}: Failed to build TTS service with error: {ErrorMessage}", _agentState.AgentId, ttsServiceResult.Message);
                throw new InvalidOperationException($"Failed to build TTS service: {ttsServiceResult.Message}");
            }

            DisposeCurrentTTSService(); // Dispose previous if any
            _agentState.TTSService = ttsServiceResult.Data;
            _agentState.TTSService.Initialize(); // Initialize the new service
            _logger.LogInformation("Agent {AgentId}: TTS service initialized/re-initialized.", _agentState.AgentId);
        }

        private async Task LoadBackgroundMusicAsync()
        {
            if (string.IsNullOrWhiteSpace(_agentState.BusinessAppAgent?.Settings?.BackgroundAudioUrl))
            {
                _logger.LogInformation("Agent {AgentId}: No background audio URL configured.", _agentState.AgentId);
                _agentState.IsBackgroundMusicEnabled = false;
                _agentState.IsBackgroundMusicLoaded = false;
                return;
            }

            string audioUrl = _agentState.BusinessAppAgent.Settings.BackgroundAudioUrl;
            _logger.LogInformation("Agent {AgentId}: Attempting to load background audio (ID: {FileId})", _agentState.AgentId, audioUrl);

            try
            {
                AudioFileResult? fileResult = await _audioRepository.GetFileWithMetadataAsync(audioUrl);

                if (fileResult == null || fileResult.Data.IsEmpty)
                {
                    _logger.LogWarning("Agent {AgentId}: Background audio file not found or is empty (ID: {FileId})", _agentState.AgentId, audioUrl);
                    _agentState.IsBackgroundMusicEnabled = false;
                    _agentState.IsBackgroundMusicLoaded = false;
                    return;
                }

                // Determine Format and Convert
                ReadOnlyMemory<byte> rawPcmData;
                string? contentType = null;
                if (fileResult.Metadata.TryGetValue("fileContentType", out contentType) && !string.IsNullOrWhiteSpace(contentType))
                {
                    _logger.LogInformation("Agent {AgentId}: Original file Content-Type is {ContentType}.", _agentState.AgentId, contentType);
                    rawPcmData = await AudioToRAWPCMConverter.ConvertToRawPcmAsync(
                        fileResult.Data,
                        contentType,
                        audioUrl,
                        _logger,
                        _agentState.AgentId,
                        SampleRate,
                        BitsPerSample,
                        Channels
                    );
                }
                else
                {
                    _logger.LogWarning("Agent {AgentId}: Background audio file (ID: {FileId}) is missing 'fileContentType' metadata. Cannot determine format.", _agentState.AgentId, audioUrl);
                    rawPcmData = ReadOnlyMemory<byte>.Empty; // Indicate failure
                }


                // Validate Conversion Result
                if (rawPcmData.IsEmpty) // Conversion failed or format was unsupported
                {
                    _logger.LogWarning("Agent {AgentId}: Failed to convert background audio (ID: {FileId}) to required PCM format.", _agentState.AgentId, audioUrl);
                    _agentState.IsBackgroundMusicEnabled = false;
                    _agentState.IsBackgroundMusicLoaded = false;
                    return;
                }

                // Check alignment for the *converted* PCM data
                if (rawPcmData.Length % BytesPerSample != 0)
                {
                    _logger.LogError("Agent {AgentId}: Converted background audio (ID: {FileId}) has invalid length ({Length} bytes). This should not happen after successful PCM conversion.",
                        _agentState.AgentId, audioUrl, rawPcmData.Length);
                    _agentState.IsBackgroundMusicEnabled = false;
                    _agentState.IsBackgroundMusicLoaded = false;
                    return;
                }

                // Success
                _agentState.BackgroundAudioData = rawPcmData;
                _logger.LogInformation("Agent {AgentId}: Background audio loaded and converted successfully ({Length} bytes of raw PCM).", _agentState.AgentId, _agentState.BackgroundAudioData.Length);
                // enable bkg audio in begin conversation
                _agentState.IsBackgroundMusicLoaded = true;
                _backgroundAudioPosition = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentId}: Error loading or converting background audio (ID: {FileId})", _agentState.AgentId, audioUrl);
                _agentState.IsBackgroundMusicEnabled = false;
                _agentState.IsBackgroundMusicLoaded = false;
            }
        }

        public async Task<(bool Success, TimeSpan Duration)> SynthesizeAndQueueSpeechAsync(string text, CancellationToken externalToken) // Called by LLM Handler
        {
            if (string.IsNullOrWhiteSpace(text) || _agentState.TTSService == null)
            {
                _logger.LogWarning("Agent {AgentId}: Cannot synthesize empty text or TTS service is null.", _agentState.AgentId);
                return (false, TimeSpan.Zero);
            }

            // Cancel previous TTS task if running, create a new CTS linked to overall shutdown and external token
            _currentTtsTaskCTS?.Cancel();
            _currentTtsTaskCTS?.Dispose();
            _currentTtsTaskCTS = CancellationTokenSource.CreateLinkedTokenSource(_audioSendingCTS.Token, externalToken); // Link to module CTS and external one
            var ttsToken = _currentTtsTaskCTS.Token;

            try
            {
                _logger.LogDebug("Agent {AgentId}: Starting TTS synthesis for text: \"{snippet}\"", _agentState.AgentId, text.Length > 50 ? text.Substring(0, 50) + "..." : text);
                var (audioData, audioDuration) = await _agentState.TTSService.SynthesizeTextAsync(text, ttsToken);

                if (ttsToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Agent {AgentId}: TTS synthesis was cancelled during generation.", _agentState.AgentId);
                    return (false, TimeSpan.Zero);
                }

                if (audioData == null || audioData.Length == 0 || audioDuration == null || audioDuration.Value <= TimeSpan.Zero)
                {
                    _logger.LogWarning("Agent {AgentId}: TTS service returned null or empty audio/duration for text: \"{Text}\"", _agentState.AgentId, text.Length > 50 ? text.Substring(0, 50) + "..." : text);
                    return (false, TimeSpan.Zero);
                }

                _agentState.CurrentResponseDuration = _agentState.CurrentResponseDuration.Add(audioDuration.Value);

                var segment = new SpeechSegment(audioData, audioDuration.Value);

                // Add to queue, respecting cancellation
                _speechAudioQueue.Add(segment, _audioSendingCTS.Token); // Use audio sending CTS for queue add

                _logger.LogDebug("Agent {AgentId}: TTS synthesis complete, duration: {Duration}, queued.", _agentState.AgentId, segment.Duration);
                return (true, segment.Duration);
            }
            catch (OperationCanceledException) when (ttsToken.IsCancellationRequested || _audioSendingCTS.Token.IsCancellationRequested)
            {
                _logger.LogInformation("Agent {AgentId}: TTS synthesis cancelled for text: \"{Text}\"", _agentState.AgentId, text.Length > 50 ? text.Substring(0, 50) + "..." : text);
                return (false, TimeSpan.Zero);
            }
            catch (InvalidOperationException) when (_speechAudioQueue.IsAddingCompleted)
            {
                _logger.LogWarning("Agent {AgentId}: Could not queue speech segment as queue is completed.", _agentState.AgentId);
                return (false, TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentId}: Error synthesizing speech for text: {Text}", _agentState.AgentId, text.Length > 50 ? text.Substring(0, 50) + "..." : text);
                // TODO: Raise error event
                return (false, TimeSpan.Zero);
            }
            finally
            {
                // Clean up the specific CTS for this TTS task
                _currentTtsTaskCTS?.Dispose();
                _currentTtsTaskCTS = null;
            }
        }

        public async Task SynthesizeAndPlayBlockingAsync(string text, CancellationToken cancellationToken)
        {
            // --- Move logic from original SynthesizeAndPlaySpeechAsync here ---
            // Calls SynthesizeAndQueueSpeechAsync
            // Waits for the duration (needs careful cancellation handling)

            if (string.IsNullOrWhiteSpace(text)) return;

            // 1. Cancel any currently playing speech first (important for blocking calls)
            await CancelCurrentSpeechPlaybackAsync();

            // 2. Synthesize and queue the new speech
            var (success, duration) = await SynthesizeAndQueueSpeechAsync(text, cancellationToken);

            // 3. Wait for the estimated duration if synthesis was successful
            if (success && duration > TimeSpan.Zero)
            {
                _logger.LogDebug("Agent {AgentId}: Blocking - waiting for estimated speech duration: {Duration}", _agentState.AgentId, duration);
                try
                {
                    // Wait for the duration, but allow cancellation
                    // Use a combined token source for waiting
                    using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _audioSendingCTS.Token);
                    await Task.Delay(duration, waitCts.Token);
                    _logger.LogDebug("Agent {AgentId}: Blocking wait finished.", _agentState.AgentId);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Agent {AgentId}: Blocking wait for speech playback cancelled.", _agentState.AgentId);
                    // If cancelled during wait, ensure playback stops
                    await CancelCurrentSpeechPlaybackAsync();
                }
            }
            else if (!success)
            {
                _logger.LogError("Agent {AgentId}: Failed to synthesize speech for blocking message: {Text}", _agentState.AgentId, text.Length > 50 ? text.Substring(0, 50) + "..." : text);
                // Handle error - maybe log, maybe try fallback?
            }
        }


        private async Task ProcessAudioSpeakingQueueAsync(CancellationToken cancellationToken)
        {
            // --- Move logic from original ProcessAudioSpeakingQueueAsync here ---
            // Loop while !cancellationToken.IsCancellationRequested
            // Check _currentSpeechSegment, take from _speechAudioQueue if empty
            // Get background chunk using GetNextBackgroundChunk
            // Mix using MixAudioChunks (reads _agentState.CurrentAgentVolumeFactor)
            // Raise AudioChunkGenerated event
            // Task.Delay(ChunkDurationMs, cancellationToken)
            _logger.LogInformation("Agent {AgentId}: Audio sending task started.", _agentState.AgentId);
            try
            {
                bool playbackWasComplete = true; // Assume initially complete
                while (!cancellationToken.IsCancellationRequested)
                {
                    _agentState.AudioDurationLeftToPlay = CurrentlyLeftToPlay();

                    byte[]? chunkToSend = null;
                    bool isSpeechChunk = false;
                    bool segmentFinished = false;

                    // --- Process Current Speech Segment ---
                    if (!_currentSpeechSegment.IsEmpty)
                    {
                        playbackWasComplete = false; // Speech is playing
                        int remainingSpeechBytes = _currentSpeechSegment.Length - _currentSpeechPosition;
                        int speechChunkSize = Math.Min(BytesPerChunk, remainingSpeechBytes);

                        if (speechChunkSize > 0)
                        {
                            var speechChunk = _currentSpeechSegment.Slice(_currentSpeechPosition, speechChunkSize);
                            var backgroundChunk = GetNextBackgroundChunk(speechChunkSize);
                            chunkToSend = MixAudioChunks(speechChunk, backgroundChunk);
                            _currentSpeechPosition += speechChunkSize;
                            isSpeechChunk = true;

                            if (_currentSpeechPosition >= _currentSpeechSegment.Length)
                            {
                                _logger.LogTrace("Agent {AgentId}: Finished sending speech segment.", _agentState.AgentId);
                                _currentSpeechSegment = ReadOnlyMemory<byte>.Empty;
                                _currentSpeechDuration = TimeSpan.Zero;
                                _currentSpeechPosition = 0;
                                segmentFinished = true; // Mark that a segment just finished
                            }
                        }
                        else // Should not happen if logic is correct
                        {
                            _currentSpeechSegment = ReadOnlyMemory<byte>.Empty;
                            _currentSpeechDuration = TimeSpan.Zero;
                            _currentSpeechPosition = 0;
                            segmentFinished = true; // Treat as finished
                        }
                    }

                    // --- If no current speech chunk, try dequeuing next segment ---
                    if (chunkToSend == null)
                    {
                        if (_speechAudioQueue.TryTake(out var nextSegment))
                        {
                            playbackWasComplete = false; // Starting new speech
                            _logger.LogTrace("Agent {AgentId}: Starting new speech segment ({Duration}).", _agentState.AgentId, nextSegment.Duration);
                            _currentSpeechSegment = nextSegment.AudioData;
                            _currentSpeechDuration = nextSegment.Duration;
                            _currentSpeechPosition = 0;

                            // Immediately process the first chunk of the new segment
                            int firstSpeechChunkSize = Math.Min(BytesPerChunk, _currentSpeechSegment.Length);
                            if (firstSpeechChunkSize > 0)
                            {
                                var speechChunk = _currentSpeechSegment.Slice(_currentSpeechPosition, firstSpeechChunkSize);
                                var backgroundChunk = GetNextBackgroundChunk(firstSpeechChunkSize);
                                chunkToSend = MixAudioChunks(speechChunk, backgroundChunk);
                                _currentSpeechPosition += firstSpeechChunkSize;
                                isSpeechChunk = true;

                                if (_currentSpeechPosition >= _currentSpeechSegment.Length) // Handle very short segments
                                {
                                    _logger.LogTrace("Agent {AgentId}: Finished sending short speech segment immediately.", _agentState.AgentId);
                                    _currentSpeechSegment = ReadOnlyMemory<byte>.Empty;
                                    _currentSpeechDuration = TimeSpan.Zero;
                                    _currentSpeechPosition = 0;
                                    segmentFinished = true;
                                }
                            }
                            else // Dequeued segment is empty
                            {
                                _logger.LogWarning("Agent {AgentId}: Dequeued speech segment has zero length.", _agentState.AgentId);
                                _currentSpeechSegment = ReadOnlyMemory<byte>.Empty;
                                _currentSpeechDuration = TimeSpan.Zero;
                                segmentFinished = true;
                            }
                        }
                        else // Queue is empty and no current segment
                        {
                            // Play background only (if enabled)
                            var backgroundChunk = GetNextBackgroundChunk(BytesPerChunk);
                            if (!backgroundChunk.IsEmpty)
                            {
                                chunkToSend = MixAudioChunks(ReadOnlyMemory<byte>.Empty, backgroundChunk); // Mix with silence
                            }

                            // Check if playback just became complete
                            if (!playbackWasComplete)
                            {
                                _logger.LogDebug("Agent {AgentId}: Speech queue empty and current segment finished.", _agentState.AgentId);
                                SpeechPlaybackComplete?.Invoke();
                                playbackWasComplete = true;
                            }
                        }
                    }

                    // --- Send Chunk and Delay ---
                    if (chunkToSend != null && chunkToSend.Length > 0)
                    {
                        try
                        {
                            // Use ToArray() if event expects byte[], otherwise pass ReadOnlyMemory if possible
                            AudioChunkGenerated?.Invoke(this, new ConversationAudioGeneratedEventArgs(chunkToSend, _agentState.CurrentClientId));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Agent {AgentId}: Error invoking AudioChunkGenerated event.", _agentState.AgentId);
                            // Decide how to handle - stop sending? Log and continue?
                        }

                        await Task.Delay(ChunkDurationMs, cancellationToken);
                    }
                    else
                    {
                        // No audio (speech or background) to send, wait briefly
                        await Task.Delay(50, cancellationToken);
                    }

                    // Check if playback became complete *after* sending the last chunk of a segment
                    if (segmentFinished && _currentSpeechSegment.IsEmpty && _speechAudioQueue.IsCompleted && !playbackWasComplete)
                    {
                        _logger.LogDebug("Agent {AgentId}: Speech queue empty immediately after finishing segment.", _agentState.AgentId);
                        SpeechPlaybackComplete?.Invoke();
                        playbackWasComplete = true;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Agent {AgentId}: Audio sending task cancelled.", _agentState.AgentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentId}: Error in audio sending task.", _agentState.AgentId);
                // TODO: Raise error event
            }
            finally
            {
                _logger.LogInformation("Agent {AgentId}: Audio sending task finished.", _agentState.AgentId);
            }
        }

        private ReadOnlyMemory<byte> GetNextBackgroundChunk(int desiredChunkSize)
        {
            if (!_agentState.IsBackgroundMusicEnabled || !_agentState.IsBackgroundMusicLoaded || _agentState.BackgroundAudioData.IsEmpty)
            {
                return ReadOnlyMemory<byte>.Empty;
            }

            int backgroundLength = _agentState.BackgroundAudioData.Length;
            int remainingBackgroundBytes = backgroundLength - _backgroundAudioPosition;
            int bytesToTake = Math.Min(desiredChunkSize, remainingBackgroundBytes);

            ReadOnlyMemory<byte> chunk;

            if (bytesToTake < desiredChunkSize) // Need to loop
            {
                var remainingChunk = _agentState.BackgroundAudioData.Slice(_backgroundAudioPosition, bytesToTake);
                _backgroundAudioPosition = 0; // Reset position
                int neededFromStart = desiredChunkSize - bytesToTake;
                neededFromStart = Math.Min(neededFromStart, backgroundLength); // Handle total data < chunk size

                if (neededFromStart > 0)
                {
                    var startChunk = _agentState.BackgroundAudioData.Slice(0, neededFromStart);
                    // Combine - requires allocation
                    var combined = new byte[bytesToTake + neededFromStart];
                    remainingChunk.CopyTo(combined.AsMemory().Slice(0, bytesToTake));
                    startChunk.CopyTo(combined.AsMemory().Slice(bytesToTake, neededFromStart));
                    chunk = combined;
                    _backgroundAudioPosition = neededFromStart; // Update position
                }
                else // Only remainingChunk was needed
                {
                    chunk = remainingChunk; // Position already reset
                }
            }
            else // Simple chunk
            {
                chunk = _agentState.BackgroundAudioData.Slice(_backgroundAudioPosition, bytesToTake);
                _backgroundAudioPosition += bytesToTake;
            }

            // Ensure position loops correctly if exactly at the end
            if (_backgroundAudioPosition >= backgroundLength)
            {
                _backgroundAudioPosition = 0;
            }

            return chunk;
        }

        private byte[] MixAudioChunks(ReadOnlyMemory<byte> speechChunk, ReadOnlyMemory<byte> backgroundChunk)
        {
            // --- Move logic from original MixAudioChunks here ---
            // Use _agentState.CurrentAgentVolumeFactor and _agentState.BackgroundMusicVolume
            int outputLength = Math.Max(speechChunk.Length, backgroundChunk.Length);
            if (outputLength == 0) return Array.Empty<byte>();
            if (outputLength % 2 != 0) outputLength++; // Ensure even length for 16-bit

            byte[] mixedOutput = new byte[outputLength];
            var mixedSpan = mixedOutput.AsSpan();

            var speechSpan = speechChunk.Span;
            var backgroundSpan = backgroundChunk.Span;

            // Ensure spans are valid before casting (non-empty and even length)
            var speechShortSpan = speechSpan.Length >= 2 && speechSpan.Length % 2 == 0
               ? MemoryMarshal.Cast<byte, short>(speechSpan)
               : ReadOnlySpan<short>.Empty;
            var backgroundShortSpan = backgroundSpan.Length >= 2 && backgroundSpan.Length % 2 == 0
               ? MemoryMarshal.Cast<byte, short>(backgroundSpan)
               : ReadOnlySpan<short>.Empty;

            var mixedShortSpan = MemoryMarshal.Cast<byte, short>(mixedSpan);

            float agentVolume = (float)_agentState.CurrentAgentVolumeFactor; // Read volatile state
            float backgroundVolume = (float)_agentState.BackgroundMusicVolume;

            for (int i = 0; i < mixedShortSpan.Length; i++)
            {
                short speechSample = i < speechShortSpan.Length ? (short)((float)speechShortSpan[i] * (float)agentVolume) : (short)0;
                short backgroundSample = i < backgroundShortSpan.Length ? (short)((float)backgroundShortSpan[i] * (float)backgroundVolume) : (short)0;

                int mixedSample = speechSample + backgroundSample;
                mixedShortSpan[i] = (short)Math.Clamp(mixedSample, short.MinValue, short.MaxValue);
            }

            return mixedOutput;
        }

        public Task StartVolumeFadeAsync(float targetFactor, TimeSpan duration)
        {
            _volumeFadeCTS?.Cancel(); // Cancel existing fade
            _volumeFadeCTS?.Dispose();
            if (_volumeFadeTask != null)
            {
                _volumeFadeTask?.Wait(500);
            }
            _volumeFadeCTS = CancellationTokenSource.CreateLinkedTokenSource(_audioSendingCTS.Token);
            var token = _volumeFadeCTS.Token;

            float startFactor = _agentState.CurrentAgentVolumeFactor; // Read current volatile value
            float target = Math.Clamp(targetFactor, 0.0f, 1.0f);
            var fadeStartTime = DateTime.UtcNow;

            _logger.LogDebug("Agent {AgentId}: Starting volume fade from {StartFactor:F2} to {TargetFactor:F2} over {Duration}",
                            _agentState.AgentId, startFactor, target, duration);

            _volumeFadeTask = Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        var elapsed = DateTime.UtcNow - fadeStartTime;
                        float progress = (float)Math.Clamp(elapsed.TotalMilliseconds / duration.TotalMilliseconds, 0.0, 1.0);

                        // Linear interpolation (Lerp)
                        var currentCalcualted = (startFactor + (target - startFactor)) * progress;
                        _agentState.CurrentAgentVolumeFactor = currentCalcualted;

                        if (progress >= 1.0f)
                        {
                            _agentState.CurrentAgentVolumeFactor = target; // Ensure exact target
                            _logger.LogTrace("Agent {AgentId}: Volume fade completed at {TargetFactor:F2}", _agentState.AgentId, target);
                            break;
                        }
                        await Task.Delay(10, token); // Check ~50 times per second
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogTrace("Agent {AgentId}: Volume fade task cancelled.", _agentState.AgentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Agent {AgentId}: Error during volume fade task.", _agentState.AgentId);
                }
                // Optionally set final value even on cancellation?
                // if (token.IsCancellationRequested) { _agentState.CurrentAgentVolumeFactor = target; }
            }, token);

            return _volumeFadeTask; // Return the task so caller can optionally await it
        }

        public async Task CancelCurrentSpeechPlaybackAsync()
        {
            // --- Move logic from original CancelCurrentSpeechPlaybackAsync here ---
            // Clear _speechAudioQueue
            // Reset _currentSpeechSegment, _currentSpeechPosition
            // Cancel _volumeFadeCTS
            // Cancel TTS task (_currentTtsTaskCTS)
            // Stop TTS service (_agentState.TTSService.Stop...)

            _logger.LogDebug("Agent {AgentId}: Cancelling current speech playback and synthesis.", _agentState.AgentId);

            // 1. Stop any ongoing TTS generation for this module
            _currentTtsTaskCTS?.Cancel();
            // Call the TTS service's stop method
            await (_agentState.TTSService?.StopTextSynthesisAsync() ?? Task.CompletedTask);


            // 2. Cancel any ongoing volume fade
            _volumeFadeCTS?.Cancel();

            // 3. Clear the queue of pending segments
            while (_speechAudioQueue.TryTake(out _)) { }

            // 4. Reset current segment playback state
            _currentSpeechSegment = ReadOnlyMemory<byte>.Empty;
            _currentSpeechPosition = 0;

            // 5. Signal playback is complete (as we just cleared everything)
            SpeechPlaybackComplete?.Invoke();

            // We don't cancel _audioSendingCTS here, as that stops the whole loop including background.
            // The loop will naturally stop playing speech as the queue/current segment are empty.
            // If background music should also stop, that needs separate logic.
            _logger.LogDebug("Agent {AgentId}: Speech playback cancelled.", _agentState.AgentId);
        }

        public void StopSending() // Called during shutdown
        {
            _speechAudioQueue.CompleteAdding();
            _audioSendingCTS.Cancel(); // Signal the sending loop to terminate
            _volumeFadeCTS?.Cancel(); // Stop fades
            _currentTtsTaskCTS?.Cancel(); // Stop any final TTS
        }

        private void DisposeCurrentTTSService()
        {
            if (_agentState.TTSService != null)
            {
                _logger.LogDebug("Disposing existing TTS service for Agent {AgentId}.", _agentState.AgentId);
                try
                {
                    // Dispose if implements IDisposable
                    (_agentState.TTSService as IDisposable)?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Agent {AgentId}: Exception disposing current TTS service.", _agentState.AgentId);
                }
                _agentState.TTSService = null;
            }
        }

        public TimeSpan CurrentlyLeftToPlay()
        {
            TimeSpan totalDuration = TimeSpan.Zero;
            foreach (var segment in _speechAudioQueue)
            {
                totalDuration += segment.Duration;
            }

            if (_currentSpeechSegment.Length != 0 && _currentSpeechDuration != TimeSpan.Zero)
            {
                int currentSegmentDurationLeft = (int)((int)_currentSpeechDuration.TotalMilliseconds * (int)_currentSpeechPosition) / (int)_currentSpeechSegment.Length;
                totalDuration = totalDuration.Add(TimeSpan.FromMilliseconds((int)currentSegmentDurationLeft));
            }     

            return totalDuration;
        }

        public void Dispose()
        {
            StopSending();
            DisposeCurrentTTSService();
            _audioSendingTask?.Wait(TimeSpan.FromSeconds(2)); // Wait for task to finish
            _speechAudioQueue?.Dispose();
            _audioSendingCTS?.Dispose();
            _volumeFadeCTS?.Dispose();
            _currentTtsTaskCTS?.Dispose();
            _logger.LogDebug("AudioOutput module disposed for Agent {AgentId}.", _agentState.AgentId);
        }
    }
}