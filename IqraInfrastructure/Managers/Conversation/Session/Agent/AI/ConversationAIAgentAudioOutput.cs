using IqraCore.Entities.Business;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Conversation.Turn;
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
        public string Id { get; }
        public string Text { get; }
        public ReadOnlyMemory<byte> AudioData { get; }
        public TimeSpan Duration { get; }
        public string TurnId { get; }

        public SpeechSegment(string id, string text, ReadOnlyMemory<byte> audioData, TimeSpan duration, string turnId)
        {
            Id = id;
            Text = text;
            AudioData = audioData;
            Duration = duration;
            TurnId = turnId;
        }
    }

    public class ConversationAIAgentAudioOutput : IDisposable
    {
        public event EventHandler<ConversationAudioGeneratedEventArgs>? AudioChunkGenerated;
        public event EventHandler<ConversationTurn>? AgentResponsePlaybackComplete;
        public event EventHandler<object?>? OnAudioBufferCleared;
        public event EventHandler<ConversationTurn>? TurnUpdate;

        private readonly ILogger<ConversationAIAgentAudioOutput> _logger;
        private readonly ConversationSessionOrchestrator _conversationSession;
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
        private string _currentSpeechSegmentId = string.Empty;
        private ReadOnlyMemory<byte> _currentSpeechSegmentAudio = ReadOnlyMemory<byte>.Empty;
        private TimeSpan _currentSpeechSegmentDuration = TimeSpan.Zero;
        private string _currentSpeechSegmentText = string.Empty;
        private int _currentSpeechSegmentAudioPosition = 0;
        private volatile bool _isPlaybackPaused = false;

        // TTS Specific Task Management
        private CancellationTokenSource? _currentTtsTaskCTS = null;

        public ConversationAIAgentAudioOutput(
            ILoggerFactory loggerFactory,
            ConversationAIAgentState agentState,
            TTSProviderManager ttsProviderManager,
            BusinessAgentAudioRepository audioRepository,
            BusinessManager businessManager,
            TTSAudioCacheManager cacheManager,
            ConversationSessionOrchestrator conversationSession
        )
        {
            _logger = loggerFactory.CreateLogger<ConversationAIAgentAudioOutput>();
            _agentState = agentState;
            _ttsProviderManager = ttsProviderManager;
            _audioRepository = audioRepository;
            _businessManager = businessManager;
            _cacheManager = cacheManager;
            _conversationSession = conversationSession;
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
        public void StartProcessingAudioTask()
        {
            if (_audioSendingTask != null) return;

            _audioSendingTask = Task.Run(() => ProcessAudioSpeakingQueueAsync(_audioSendingCTS.Token), _audioSendingCTS.Token);
        }
        public async Task<(bool Success, TimeSpan Duration)> SynthesizeAndQueueSpeechAsync(ConversationTurn turn, string text, bool markTurnAsCompleteAfterThis, CancellationToken externalToken) // Called by LLM Handler
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
                        var cachedSegment = new SpeechSegment(Guid.NewGuid().ToString(), text, cacheResult.AudioData, cacheResult.Duration, turn.Id);
                        _speechAudioQueue.Add(cachedSegment, _audioSendingCTS.Token);
                        _speechAudioQueue.CompleteAdding();
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

                var segment = new SpeechSegment(Guid.NewGuid().ToString(), text, audioData, audioDuration.Value, turn.Id);
                _speechAudioQueue.Add(segment, _audioSendingCTS.Token);
                _speechAudioQueue.CompleteAdding();

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
        public async Task SynthesizeAndPlayBlockingAsync(ConversationTurn turn, string text, CancellationToken cancellationToken)
        {
            // --- Move logic from original SynthesizeAndPlaySpeechAsync here ---
            // Calls SynthesizeAndQueueSpeechAsync
            // Waits for the duration (needs careful cancellation handling)

            if (string.IsNullOrWhiteSpace(text)) return;

            // 1. Cancel any currently playing speech first (important for blocking calls)
            await CancelCurrentSpeechPlaybackAsync();

            // 2. Synthesize and queue the new speech
            var (success, duration) = await SynthesizeAndQueueSpeechAsync(turn, text, true, cancellationToken);

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
            _currentTtsTaskCTS?.Cancel();
            await (_agentState.TTSService?.StopTextSynthesisAsync() ?? Task.CompletedTask);

            while (_speechAudioQueue.TryTake(out _)) { }

            var turnBeingCancelled = _agentState.CurrentTurn;
            if (turnBeingCancelled != null && turnBeingCancelled.Response.Type == AgentResponseType.Speech)
            {
                // Find the segment that was actively playing when cancellation was called.
                var activeSegment = turnBeingCancelled.Response.SpokenSegments
                                        .FirstOrDefault(s => s.StartedPlayingAt != default && s.FinishedPlayingAt == null);

                if (activeSegment != null)
                {
                    activeSegment.WasInterrupted = true;
                    activeSegment.FinishedPlayingAt = DateTime.UtcNow; // Mark the exact time of interruption
                }

                // Update the overall response status
                // If nothing has been played, it was cancelled. If something has played, it was interrupted.
                bool hasPlayedSomething = turnBeingCancelled.Response.SpokenSegments.Any(s => s.StartedPlayingAt != default);
                turnBeingCancelled.Response.Status = hasPlayedSomething ? AgentResponseStatus.Interrupted : AgentResponseStatus.Cancelled;
                turnBeingCancelled.Response.SpeechCompletedAt = DateTime.UtcNow;

                TurnUpdate?.Invoke(this, turnBeingCancelled); // Persist the final interrupted state
            }

            // Reset local playback state
            _currentSpeechSegmentId = string.Empty;
            _currentSpeechSegmentAudio = ReadOnlyMemory<byte>.Empty;
            _currentSpeechSegmentAudioPosition = 0;
            _currentSpeechSegmentText = string.Empty;
            _isPlaybackPaused = false;

            OnAudioBufferCleared?.Invoke(this, null);
        }
        public void StopSending()
        {
            _speechAudioQueue.CompleteAdding();
            _audioSendingCTS.Cancel(); // Signal the sending loop to terminate
            _currentTtsTaskCTS?.Cancel(); // Stop any final TTS
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
                while (!cancellationToken.IsCancellationRequested)
                {
                    var currentTurn = _agentState.CurrentTurn;
                    if (currentTurn == null)
                    {
                        _logger.LogError("Agent {AgentId}: No current turn found.", _agentState.AgentId);
                        continue;
                    }

                    byte[]? chunkToSend = null;

                    // --- Process Current Speech Segment ---
                    if (!_currentSpeechSegmentAudio.IsEmpty && !_isPlaybackPaused)
                    {
                        int remainingSpeechBytes = _currentSpeechSegmentAudio.Length - _currentSpeechSegmentAudioPosition;
                        int speechChunkSize = Math.Min(BytesPerChunk, remainingSpeechBytes);

                        if (speechChunkSize > 0)
                        {
                            var speechChunk = _currentSpeechSegmentAudio.Slice(_currentSpeechSegmentAudioPosition, speechChunkSize);
                            var backgroundChunk = GetNextBackgroundChunk(speechChunkSize);
                            chunkToSend = MixAudioChunksWhileApplyingVolumeAndClipping(speechChunk, backgroundChunk);

                            _currentSpeechSegmentAudioPosition += speechChunkSize;

                            if (_currentSpeechSegmentAudioPosition >= _currentSpeechSegmentAudio.Length)
                            {
                                var segmentDataToUpdate = currentTurn.Response.SpokenSegments.FirstOrDefault(s => s.Id == s.Id);
                                if (segmentDataToUpdate != null)
                                {
                                    segmentDataToUpdate.FinishedPlayingAt = DateTime.UtcNow;
                                    TurnUpdate?.Invoke(this, currentTurn);
                                }
                            }
                        }
                        else { _logger.LogError("Agent {AgentId}: Speech chunk size is zero.", _agentState.AgentId); } // Should not happen if logic is correct
                    }

                    // If no current speech chunk, try dequeuing next segment
                    if (chunkToSend == null && !_isPlaybackPaused && _speechAudioQueue.TryTake(out var nextSegment))
                    {
                        if (currentTurn.Id != nextSegment.TurnId)
                        {
                            _logger.LogError("Agent {AgentId}: Dequeued speech segment for turn {TurnId} but current turn is {CurrentTurnId}.", _agentState.AgentId, nextSegment.TurnId, currentTurn.Id);
                            continue;
                        }

                        var newSegmentData = new SpeechSegmentData
                        {
                            Id = nextSegment.Id,
                            Text = nextSegment.Text,
                            Duration = nextSegment.Duration,
                            StartedPlayingAt = DateTime.UtcNow
                        };
                        currentTurn.Response.SpokenSegments.Add(newSegmentData);
                        TurnUpdate?.Invoke(this, currentTurn);

                        _currentSpeechSegmentId = nextSegment.Id;
                        _currentSpeechSegmentAudio = nextSegment.AudioData;
                        _currentSpeechSegmentDuration = nextSegment.Duration;
                        _currentSpeechSegmentText = nextSegment.Text;
                        _currentSpeechSegmentAudioPosition = 0;

                        // Immediately process the first chunk of the new segment
                        int firstSpeechChunkSize = Math.Min(BytesPerChunk, _currentSpeechSegmentAudio.Length);
                        if (firstSpeechChunkSize > 0)
                        {
                            var speechChunk = _currentSpeechSegmentAudio.Slice(_currentSpeechSegmentAudioPosition, firstSpeechChunkSize);
                            var backgroundChunk = GetNextBackgroundChunk(firstSpeechChunkSize);
                            chunkToSend = MixAudioChunksWhileApplyingVolumeAndClipping(speechChunk, backgroundChunk);

                            _currentSpeechSegmentAudioPosition += firstSpeechChunkSize;

                            if (_currentSpeechSegmentAudioPosition >= _currentSpeechSegmentAudio.Length)
                            {
                                var segmentDataToUpdate = currentTurn.Response.SpokenSegments.FirstOrDefault(s => s.Id == s.Id);
                                if (segmentDataToUpdate != null)
                                {
                                    segmentDataToUpdate.FinishedPlayingAt = DateTime.UtcNow;
                                    TurnUpdate?.Invoke(this, currentTurn);
                                }
                            }
                        }
                        else { _logger.LogError("Agent {AgentId}: Dequeued speech segment for turn {TurnId} has no audio data.", _agentState.AgentId, nextSegment.TurnId); } // Should not happen if logic is correct
                    }

                    if (chunkToSend == null || chunkToSend.Length == 0) // Queue is empty and no current segment / or is paused
                    {
                        // Play background only (if enabled)
                        var backgroundChunk = GetNextBackgroundChunk(BytesPerChunk);
                        if (!backgroundChunk.IsEmpty)
                        {
                            chunkToSend = MixAudioChunksWhileApplyingVolumeAndClipping(ReadOnlyMemory<byte>.Empty, backgroundChunk);
                        }
                    }

                    // --- Send Chunk and Delay ---
                    if (chunkToSend != null && chunkToSend.Length > 0)
                    {
                        AudioChunkGenerated?.Invoke(this, new ConversationAudioGeneratedEventArgs(chunkToSend, _agentState.CurrentClientId));

                        // todo this can be wrong is duration ms is smaller than the chunk size,
                        // possible when 600ms is defined but array only had 200ms left thats being played
                        // this is a temporary implementation, we must make sure it is correct and works
                        var currentChunkDuration = (int)((int)chunkToSend.Length / (int)(SampleRate * BitsPerSample / 8.0f));
                        await Task.Delay(Math.Min(ChunkDurationMs, currentChunkDuration), cancellationToken);
                    }
                    else
                    {
                        // No audio (speech or background) to send, wait briefly
                        await Task.Delay(10, cancellationToken);
                    }

                    // Check if current turn playback is complete
                    if (
                        currentTurn.Response.Status == AgentResponseStatus.Processing &&
                        currentTurn.Response.Type == AgentResponseType.Speech &&
                        currentTurn.Response.LLMProcessCompletedAt != null &&
                        currentTurn.Response.SpeechCompletedAt == null &&
                        _currentSpeechSegmentAudio.IsEmpty &&
                        _speechAudioQueue.IsCompleted
                    )
                    {
                        currentTurn.Response.Status = AgentResponseStatus.Completed;
                        currentTurn.Response.SpeechCompletedAt = DateTime.UtcNow;
                        AgentResponsePlaybackComplete?.Invoke(this, currentTurn);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // TODO handle the completion of turn
                _logger.LogInformation("Agent {AgentId}: Audio sending task cancelled.", _agentState.AgentId);
            }
            catch (Exception ex)
            {
                // TODO handle the completion of turn
                _logger.LogError(ex, "Agent {AgentId}: Error in audio sending task.", _agentState.AgentId);
                // TODO: Raise error event
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
            _currentTtsTaskCTS?.Dispose();
            _logger.LogDebug("AudioOutput module disposed for Agent {AgentId}.", _agentState.AgentId);
        }
    }
}