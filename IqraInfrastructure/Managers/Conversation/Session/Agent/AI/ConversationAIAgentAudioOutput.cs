using IqraCore.Entities.Business;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.TTS;
using IqraCore.Interfaces.TTS;
using IqraInfrastructure.Helpers.Audio;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.TTS;
using IqraInfrastructure.Repositories.Business;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System.Collections.Concurrent;


namespace IqraInfrastructure.Managers.Conversation.Session.Agent.AI
{
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
        public event EventHandler<ConversationAudioGeneratedEventArgs>? AudioChunkGenerated;
        public event Action? SpeechPlaybackComplete;
        public event EventHandler<object?>? OnAudioBufferCleared;

        private readonly ILogger<ConversationAIAgentAudioOutput> _logger;
        private readonly ConversationAIAgentState _agentState;
        private readonly TTSProviderManager _ttsProviderManager;
        private readonly BusinessAgentAudioRepository _audioRepository;
        private readonly BusinessManager _businessManager;
        private readonly TTSAudioCacheManager _cacheManager;

        private int SampleRate;
        private int BitsPerSample;
        private AudioEncodingTypeEnum AudioEncodingType;
        private int Channels;
        private int BytesPerSample;
        private int ChunkDurationMs;
        private int BytesPerChunk;

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
        private volatile bool _isPlaybackPaused = false;

        // Volume Fading State (managed here, affects _agentState.CurrentAgentVolumeFactor)
        private CancellationTokenSource? _volumeFadeCTS = null;
        private Task? _volumeFadeTask = null;
        private DateTime? _volumeFadeStartTime = null;

        // TTS Specific Task Management
        private CancellationTokenSource? _currentTtsTaskCTS = null;

        public ConversationAIAgentAudioOutput(
            ILoggerFactory loggerFactory,
            ConversationAIAgentState agentState,
            TTSProviderManager ttsProviderManager,
            BusinessAgentAudioRepository audioRepository,
            BusinessManager businessManager,
            TTSAudioCacheManager cacheManager
        )
        {
            _logger = loggerFactory.CreateLogger<ConversationAIAgentAudioOutput>();
            _agentState = agentState;
            _ttsProviderManager = ttsProviderManager;
            _audioRepository = audioRepository;
            _businessManager = businessManager;
            _cacheManager = cacheManager;
        }

        // Initalize
        public async Task InitializeAsync(CancellationToken agentCTS)
        {
            _audioSendingCTS = CancellationTokenSource.CreateLinkedTokenSource(agentCTS); // Link to agent shutdown

            SampleRate = _agentState.AgentConfiguration.SampleRate;
            BitsPerSample = _agentState.AgentConfiguration.BitsPerSample;
            Channels = _agentState.AgentConfiguration.Channels;
            AudioEncodingType = _agentState.AgentConfiguration.AudioEncodingType;

            BytesPerSample = BitsPerSample / 8;
            ChunkDurationMs = 600;
            BytesPerChunk = SampleRate * BytesPerSample * Channels * ChunkDurationMs / 1000;

            // --- Move logic from InitalizeTTSForLangauge here ---
            await InitializeTTSAsync(); // Extracted TTS setup

            // --- Move logic from LoadBackgroundMusicAsync here ---
            await LoadBackgroundMusicAsync(); // Extracted background music loading

            // Start audio sending task
            _audioSendingTask = Task.Run(() => ProcessAudioSpeakingQueueAsync(_audioSendingCTS.Token), _audioSendingCTS.Token);
            _logger.LogInformation("AudioOutput module initialized for Agent {AgentId}.", _agentState.AgentId);
        }
        public async Task ReInitializeForLanguageAsync()
        {
            _logger.LogInformation("Agent {AgentId}: Re-initializing Audio Output Handler for new language.", _agentState.AgentId);
            await CancelCurrentSpeechPlaybackAsync();
            await InitializeTTSAsync();
        }
        private async Task InitializeTTSAsync()
        {
            if (_agentState.BusinessAppAgent == null || string.IsNullOrEmpty(_agentState.CurrentLanguageCode) || _agentState.AgentConfiguration == null)
            {
                _logger.LogError("Agent {AgentId}: Cannot initialize TTS - missing required state.", _agentState.AgentId);
                throw new InvalidOperationException("TTS initialization requires BusinessAppAgent, LanguageCode, and AgentConfiguration.");
            }

            var defaultTTSServiceInfo = _agentState.BusinessAppAgent.Integrations.TTS[_agentState.CurrentLanguageCode][0];
            var ttsBusinessIntegrationDataResult = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(_agentState.BusinessApp.Id, defaultTTSServiceInfo.Id);

            if (!ttsBusinessIntegrationDataResult.Success || ttsBusinessIntegrationDataResult.Data == null)
            {
                _logger.LogError("Agent {AgentId}: Business app TTS integration {IntegrationId} not found", _agentState.AgentId, defaultTTSServiceInfo.Id);
                throw new InvalidOperationException($"Business app TTS integration {defaultTTSServiceInfo.Id} not found");
            }
            _agentState.TTSBusinessIntegrationData = ttsBusinessIntegrationDataResult.Data;

            var ttsServiceResult = await _ttsProviderManager.BuildProviderServiceByIntegration(_agentState.TTSBusinessIntegrationData, defaultTTSServiceInfo, _agentState.AgentConfiguration.SampleRate, _agentState.AgentConfiguration.BitsPerSample, _agentState.AgentConfiguration.AudioEncodingType);
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
                    AudioEncodingTypeEnum? backgroundFileEncoding = null;
                    if (contentType == "audio/mpeg")
                    {
                        backgroundFileEncoding = AudioEncodingTypeEnum.MPEG;
                    }
                    else if (contentType == "audio/wav")
                    {
                        backgroundFileEncoding = AudioEncodingTypeEnum.WAV;
                    }
                    else
                    {
                        _logger.LogWarning("Agent {AgentId}: Background audio file (ID: {FileId}) has unsupported format '{ContentType}'.", _agentState.AgentId, audioUrl, contentType);
                    }

                    if (backgroundFileEncoding != null)
                    {
                        var convertedBKGAudio = AudioConversationHelper.Convert(
                            fileResult.Data.ToArray(),
                            new TTSProviderAvailableAudioFormat()
                            {
                                Encoding = backgroundFileEncoding.Value,
                                BitsPerSample = 0, // for wav and mpeg, its in the header
                                SampleRateHz = 0 // for wav and mpeg, its in the header
                            },
                            new AudioRequestDetails()
                            {
                                RequestedEncoding = _agentState.AgentConfiguration.AudioEncodingType,
                                RequestedBitsPerSample = _agentState.AgentConfiguration.BitsPerSample,
                                RequestedSampleRateHz = _agentState.AgentConfiguration.SampleRate
                            },
                            false
                        );

                        rawPcmData = convertedBKGAudio.audioData;
                    }
                    else
                    {
                        rawPcmData = ReadOnlyMemory<byte>.Empty; // Indicate failure
                    }
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

        // Management
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
                (bool isCacheable, string? cacheGroupId, string? cacheEntryId) = await IsTextCacheable(text);
                string cacheKey = string.Empty;
                ITTSConfig ttsConfig = _agentState.TTSService.GetCacheableConfig();

                if (isCacheable)
                {
                    var cacheResult = await _cacheManager.TryGetAudioAsync(cacheKey, ttsConfig, _agentState.TTSService.GetProviderType(), _agentState.BusinessApp.Id, cacheGroupId, _agentState.CurrentLanguageCode, cacheEntryId, ttsToken);
                    if (cacheResult.IsHit && !cacheResult.AudioData.IsEmpty)
                    {
                        var cachedSegment = new SpeechSegment(cacheResult.AudioData, cacheResult.Duration);
                        _speechAudioQueue.Add(cachedSegment, _audioSendingCTS.Token);
                        return (true, cachedSegment.Duration);
                    }
                }

                var (audioData, audioDuration) = await _agentState.TTSService.SynthesizeTextAsync(text, ttsToken, null);
                if (ttsToken.IsCancellationRequested)
                {
                    return (false, TimeSpan.Zero);
                }

                if (audioData == null || audioData.Length == 0 || audioDuration == null || audioDuration.Value <= TimeSpan.Zero)
                {
                    _logger.LogWarning("Agent {AgentId}: TTS service returned null or empty audio/duration for text: \"{Text}\"", _agentState.AgentId, text.Length > 50 ? text.Substring(0, 50) + "..." : text);
                    return (false, TimeSpan.Zero);
                }

                if (isCacheable && audioData.Length != 0)
                {
                    _ = _cacheManager.StoreAudioAsync(
                        cacheKey,
                        audioData,
                        audioDuration.Value,
                        ttsConfig,
                        _agentState.TTSService.GetProviderType(),
                        _agentState.BusinessApp.Id,
                        cacheGroupId,
                        _agentState.CurrentLanguageCode,
                        cacheEntryId,
                        ttsToken
                    );
                }

                var segment = new SpeechSegment(audioData, audioDuration.Value);
                _speechAudioQueue.Add(segment, _audioSendingCTS.Token);
                _agentState.CurrentResponseDuration = _agentState.CurrentResponseDuration.Add(audioDuration.Value);

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
                    await Task.Delay((int)duration.TotalMilliseconds, waitCts.Token);
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
        public async Task CancelCurrentSpeechPlaybackAsync()
        {
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
            _currentSpeechDuration = TimeSpan.Zero;
            _isPlaybackPaused = false;

            // 5. Signal playback is complete (as we just cleared everything)
            OnAudioBufferCleared?.Invoke(this, null);
            SpeechPlaybackComplete?.Invoke();

            // We don't cancel _audioSendingCTS here, as that stops the whole loop including background.
            // The loop will naturally stop playing speech as the queue/current segment are empty.
            // If background music should also stop, that needs separate logic.
            _logger.LogDebug("Agent {AgentId}: Speech playback cancelled.", _agentState.AgentId);
        }
        public void StopSending()
        {
            _speechAudioQueue.CompleteAdding();
            _audioSendingCTS.Cancel(); // Signal the sending loop to terminate
            _volumeFadeCTS?.Cancel(); // Stop fades
            _currentTtsTaskCTS?.Cancel(); // Stop any final TTS
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
                int currentSegmentDurationLeft = (int)_currentSpeechDuration.TotalMilliseconds * _currentSpeechPosition / _currentSpeechSegment.Length;
                totalDuration = totalDuration.Add(TimeSpan.FromMilliseconds(currentSegmentDurationLeft));
            }

            return totalDuration;
        }
        public Task PausePlaybackAsync()
        {
            if (!_isPlaybackPaused)
            {
                _logger.LogInformation("Agent {AgentId}: Pausing audio playback.", _agentState.AgentId);
                _isPlaybackPaused = true;
                // Optionally, we could add a quick volume fade-out here if desired.
                // await StartVolumeFadeAsync(0.0f, TimeSpan.FromMilliseconds(100), CancellationToken.None);
            }
            return Task.CompletedTask;
        }
        public Task ResumePlaybackAsync()
        {
            if (_isPlaybackPaused)
            {
                _logger.LogInformation("Agent {AgentId}: Resuming audio playback.", _agentState.AgentId);
                _isPlaybackPaused = false;
                // Optionally, we could add a quick volume fade-in here.
                // await StartVolumeFadeAsync(1.0f, TimeSpan.FromMilliseconds(100), CancellationToken.None);
            }
            return Task.CompletedTask;
        }

        // Audio Processing
        private async Task<(bool isHit, string? cacheGroupId, string? cacheEntryId)> IsTextCacheable(string text)
        {
            var agent = _agentState.BusinessAppAgent;
            if (agent == null) return (false, null, null);

            // --- Step 1: Check manually assigned cache groups (existing logic) ---
            var manuallyAssignedGroupIds = agent.Cache.Audios;
            if (manuallyAssignedGroupIds != null && manuallyAssignedGroupIds.Any())
            {
                var audioCacheGroups = _agentState.BusinessApp.Cache.AudioGroups
                    .Where(g => manuallyAssignedGroupIds.Contains(g.Id));

                foreach (var group in audioCacheGroups)
                {
                    if (group.Audios.TryGetValue(_agentState.CurrentLanguageCode, out var audioList))
                    {
                        foreach (var audio in audioList)
                        {
                            if (audio.Query.Equals(text, StringComparison.OrdinalIgnoreCase))
                            {
                                return (true, group.Id, audio.Id);
                            }
                        }
                    }
                }
            }

            // --- Step 2: Check if auto-caching is enabled ---
            var autoCacheSettings = agent.Cache.AudioCacheSettings;
            if (autoCacheSettings == null ||
                !autoCacheSettings.AutoCacheAudioResponses ||
                string.IsNullOrWhiteSpace(autoCacheSettings.AutoCacheAudioResponseCacheGroupId))
            {
                _logger.LogTrace("Agent {AgentId}: Text '{Text}' is not cacheable (auto-cache disabled or not configured).", _agentState.AgentId, text);
                return (false, null, null);
            }

            // --- Step 3: Handle auto-caching logic ---
            var autoCacheGroupId = autoCacheSettings.AutoCacheAudioResponseCacheGroupId;
            var autoCacheGroup = _agentState.BusinessApp.Cache.AudioGroups.FirstOrDefault(g => g.Id == autoCacheGroupId);

            if (autoCacheGroup == null)
            {
                _logger.LogWarning("Agent {AgentId}: Auto-cache group ID '{GroupId}' not found in BusinessApp.", _agentState.AgentId, autoCacheGroupId);
                return (false, null, null);
            }

            // Ensure the language list exists for the current language
            if (!autoCacheGroup.Audios.ContainsKey(_agentState.CurrentLanguageCode))
            {
                autoCacheGroup.Audios[_agentState.CurrentLanguageCode] = new List<BusinessAppCacheAudio>();
            }

            // Check if the query already exists in this specific auto-cache group
            var existingQuery = autoCacheGroup.Audios[_agentState.CurrentLanguageCode]
                .FirstOrDefault(q => q.Query.Equals(text, StringComparison.OrdinalIgnoreCase));

            if (existingQuery != null)
            {
                // The query was likely added by another process, but it is now cacheable.
                _logger.LogTrace("Agent {AgentId}: Text '{Text}' is eligible for caching (already in auto-cache group).", _agentState.AgentId, text);
                return (true, autoCacheGroupId, existingQuery.Id);
            }
            else
            {
                // The query does NOT exist, so we add it.
                _logger.LogInformation("Agent {AgentId}: Auto-caching new query '{Text}' to group '{GroupId}'.", _agentState.AgentId, text, autoCacheGroupId);

                var newCacheAudio = new BusinessAppCacheAudio
                {
                    Id = ObjectId.GenerateNewId().ToString(), // Generate a new unique ID
                    Query = text,
                    UnusedExpiryHours = autoCacheSettings.AutoCacheAudioResponsesDefaultExpiryHours ?? 24
                };

                // This is the "write" operation. We call the repository to persist the change.
                // We can do this in a fire-and-forget manner to not slow down the current request.
                // However, the local BusinessApp object in _agentState needs to be updated too.

                var businessId = _agentState.BusinessApp.Id;
                var languageCode = _agentState.CurrentLanguageCode;

                // Persist the change to the database in the background
                _ = _businessManager.GetCacheManager().AddAudioCacheGroupAudio(businessId, autoCacheGroupId, languageCode, newCacheAudio)
                    .ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            _logger.LogError(task.Exception, "Failed to persist auto-cached audio query to database.");
                        }
                        else if (task.IsCompletedSuccessfully)
                        {
                            var taskResult = task.Result;
                            if (taskResult.Success)
                            {
                                autoCacheGroup.Audios[languageCode].Add(taskResult.Data);
                            }
                            else
                            {
                                _logger.LogError("Failed to add auto-cached audio query to group {GroupId} for Agent {AgentId}: {ErrorMessage}", autoCacheGroupId, _agentState.AgentId, taskResult.Message);
                            }
                        }
                    });

                return (true, autoCacheGroupId, newCacheAudio.Id);
            }
        }
        private async Task ProcessAudioSpeakingQueueAsync(CancellationToken cancellationToken)
        {
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
                    if (!_currentSpeechSegment.IsEmpty && !_isPlaybackPaused)
                    {
                        playbackWasComplete = false; // Speech is playing
                        int remainingSpeechBytes = _currentSpeechSegment.Length - _currentSpeechPosition;
                        int speechChunkSize = Math.Min(BytesPerChunk, remainingSpeechBytes);

                        if (speechChunkSize > 0)
                        {
                            var speechChunk = _currentSpeechSegment.Slice(_currentSpeechPosition, speechChunkSize);
                            var backgroundChunk = GetNextBackgroundChunk(speechChunkSize);
                            chunkToSend = MixAudioChunksWhileApplyingVolumeAndClipping(speechChunk, backgroundChunk);

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
                    if (chunkToSend == null && !_isPlaybackPaused)
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
                                chunkToSend = MixAudioChunksWhileApplyingVolumeAndClipping(speechChunk, backgroundChunk);

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
                        else // Queue is empty and no current segment / or is paused
                        {
                            // Play background only (if enabled)
                            var backgroundChunk = GetNextBackgroundChunk(BytesPerChunk);
                            if (!backgroundChunk.IsEmpty)
                            {
                                chunkToSend = MixAudioChunksWhileApplyingVolumeAndClipping(ReadOnlyMemory<byte>.Empty, backgroundChunk);
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
                        await Task.Delay(10, cancellationToken);
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
        private byte[] MixAudioChunksWhileApplyingVolumeAndClipping(ReadOnlyMemory<byte> speechChunk, ReadOnlyMemory<byte> backgroundChunk)
        {
            try
            {
                return AudioConversationHelper.MixAudioChunks(AudioEncodingType, SampleRate, BitsPerSample, speechChunk, _agentState.CurrentAgentVolumeFactor, backgroundChunk, _agentState.BackgroundMusicVolume);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentId}: Error mixing audio chunks.", _agentState.AgentId);

                if (speechChunk.IsEmpty && backgroundChunk.IsEmpty)
                {
                    return Array.Empty<byte>();
                }

                if (!speechChunk.IsEmpty && !backgroundChunk.IsEmpty)
                {
                    return speechChunk.ToArray();
                }

                if (speechChunk.IsEmpty)
                {
                    return backgroundChunk.ToArray();
                }

                if (backgroundChunk.IsEmpty)
                {
                    return speechChunk.ToArray();
                }

                return new byte[0];
            }
        }

        public Task StartVolumeFadeAsync(float targetFactor, TimeSpan duration, CancellationToken cancellationToken)
        {
            // this causes more issues than it solves
            return Task.CompletedTask;

            float target = Math.Clamp(targetFactor, 0.0f, 1.0f);

            if (duration <= TimeSpan.Zero)
            {
                _logger.LogDebug("Agent {AgentId}: Duration is zero or negative. Setting volume instantly to {TargetFactor:F2}", _agentState.AgentId, target);
                _agentState.CurrentAgentVolumeFactor = target;
                _volumeFadeCTS.Dispose(); // Dispose the unused CTS
                _volumeFadeCTS = null;
                return Task.CompletedTask; // Return an already completed task
            }

            _volumeFadeCTS?.Cancel(); // Cancel existing fade
            _volumeFadeCTS?.Dispose();
            if (_volumeFadeTask != null)
            {
                try {
                    _volumeFadeTask?.Wait(500);
                }
                catch (TaskCanceledException ex)
                {
                    //expected
                }
            }
            _volumeFadeStartTime = DateTime.UtcNow;
            _volumeFadeCTS = CancellationTokenSource.CreateLinkedTokenSource(_audioSendingCTS.Token, cancellationToken);
            var token = _volumeFadeCTS.Token;

            float startFactor = _agentState.CurrentAgentVolumeFactor; // Read current volatile value
            

            _logger.LogDebug("Agent {AgentId}: Starting volume fade from {StartFactor:F2} to {TargetFactor:F2} over {Duration}",
                            _agentState.AgentId, startFactor, target, duration);

            _volumeFadeTask = Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        var elapsed = DateTime.UtcNow - _volumeFadeStartTime.Value;
                        float progress;
                        double totalDurationMs = duration.TotalMilliseconds;
                        if (totalDurationMs <= 0)
                        { 
                            progress = 1.0f;
                        }
                        else
                        {
                            progress = (float)Math.Clamp(elapsed.TotalMilliseconds / totalDurationMs, 0.0, 1.0);
                        }


                        // Linear interpolation (Lerp)
                        float currentCalculated = startFactor + (target - startFactor) * progress;
                        _agentState.CurrentAgentVolumeFactor = currentCalculated;

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

        // Disposal
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