using IqraCore.Entities.Business;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Conversation.Turn;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.TTS;
using IqraCore.Interfaces.TTS;
using IqraInfrastructure.Helpers.Audio;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Conversation.Session.Logger;
using IqraInfrastructure.Managers.TTS;
using IqraInfrastructure.Managers.TTS.Helpers;
using IqraInfrastructure.Repositories.Business;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System.Collections.Concurrent;
using System.Diagnostics;


namespace IqraInfrastructure.Managers.Conversation.Session.Agent.AI
{
    internal readonly struct SpeechSegment
    {
        public string Id { get; }
        public string Text { get; }
        public ReadOnlyMemory<byte> AudioData { get; }
        public TimeSpan Duration { get; }
        public string TurnId { get; }
        public bool IsCacheHit { get; }
        public int RetrievalLatencyMS { get; }

        public SpeechSegment(string id, string text, ReadOnlyMemory<byte> audioData, TimeSpan duration, string turnId, bool isCacheHit, int retrieveLatencyMS)
        {
            Id = id;
            Text = text;
            AudioData = audioData;
            Duration = duration;
            TurnId = turnId;
            IsCacheHit = isCacheHit;
            RetrievalLatencyMS = retrieveLatencyMS;
        }
    }

    public class ConversationAIAgentAudioOutput : IDisposable
    {
        public event EventHandler<ConversationAudioGeneratedEventArgs>? AudioChunkGenerated;
        public event EventHandler<ConversationTurn>? AgentResponsePlaybackComplete;
        public event EventHandler<object?>? OnAudioBufferCleared;
        public event EventHandler<ConversationTurn>? TurnUpdate;

        private readonly SessionLoggerFactory _sessionLoggerFactory;
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
        private int FrameDurationMs;
        private int MaxBufferAheadMs;
        private int InitialSegmentDurationMs;
        private int FirstBytesPerChunk;
        private int BytesPerChunk;

        // Virtual Clock State
        private DateTime? _turnStreamingStartedAt = null;
        private TimeSpan _totalAudioDurationSent = TimeSpan.Zero;

        // Queues & Tasks
        private BlockingCollection<SpeechSegment> _speechAudioQueue = new(new ConcurrentQueue<SpeechSegment>());
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
        private DateTime? _playbackPausedAt = null;

        private bool _isFirstBackgroundAudioChunkWithoutSpeech = true;

        // TTS Specific Task Management
        private CancellationTokenSource? _currentTtsTaskCTS = null;

        // Synthesis Semaphore
        private SemaphoreSlim _synthesisSemaphore = new SemaphoreSlim(1, 1);

        public ConversationAIAgentAudioOutput(
            SessionLoggerFactory sessionLoggerFactory,
            ConversationAIAgentState agentState,
            TTSProviderManager ttsProviderManager,
            BusinessAgentAudioRepository audioRepository,
            BusinessManager businessManager,
            TTSAudioCacheManager cacheManager,
            ConversationSessionOrchestrator conversationSession
        )
        {
            _sessionLoggerFactory = sessionLoggerFactory;
            _logger = sessionLoggerFactory.CreateLogger<ConversationAIAgentAudioOutput>();
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

            // todo introduce this config into audio configuration for web session to manually define them,
            // for twilio and etc we can calculate manually based on our server vs telephony server
            FrameDurationMs = 60;
            MaxBufferAheadMs = 150;
            InitialSegmentDurationMs = 300;

            BytesPerChunk = SampleRate * BytesPerSample * Channels * FrameDurationMs / 1000;
            FirstBytesPerChunk = SampleRate * BytesPerSample * Channels * InitialSegmentDurationMs / 1000;

            await InitializeTTSAsync();
            await LoadBackgroundMusicAsync();

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

            var ttsServiceResult = await _ttsProviderManager.BuildProviderServiceByIntegration(_sessionLoggerFactory, _agentState.TTSBusinessIntegrationData, defaultTTSServiceInfo, _agentState.AgentConfiguration.SampleRate, _agentState.AgentConfiguration.BitsPerSample, _agentState.AgentConfiguration.AudioEncodingType);
            if (!ttsServiceResult.Success || ttsServiceResult.Data == null)
            {
                _logger.LogError("Agent {AgentId}: Failed to build TTS service with error: {ErrorMessage}", _agentState.AgentId, ttsServiceResult.Message);
                throw new InvalidOperationException($"Failed to build TTS service: {ttsServiceResult.Message}");
            }

            DisposeCurrentTTSService(); // Dispose previous if any
            _agentState.TTSService = ttsServiceResult.Data;
            var ttsServiceInitResult = await _agentState.TTSService.Initialize();
            if (!ttsServiceInitResult.Success)
            {
                _logger.LogError("Agent {AgentId}: Failed to initialize TTS service with error: {ErrorMessage}", _agentState.AgentId, ttsServiceInitResult.Message);
                throw new InvalidOperationException($"Failed to initialize TTS service: [{ttsServiceInitResult.Code}] {ttsServiceInitResult.Message}");
            }

            _logger.LogInformation("Agent {AgentId}: TTS service initialized/re-initialized.", _agentState.AgentId);
        }
        private async Task LoadBackgroundMusicAsync()
        {
            if (_agentState.BusinessAppAgent?.Settings?.BackgroundAudioS3StorageLink == null)
            {
                _agentState.IsBackgroundMusicEnabled = false;
                _agentState.IsBackgroundMusicLoaded = false;
                return;
            }

            string audioUrl = _agentState.BusinessAppAgent.Settings.BackgroundAudioS3StorageLink.ObjectName;
            string audioUrlOriginRegion = _agentState.BusinessAppAgent.Settings.BackgroundAudioS3StorageLink.OriginRegion;
            _logger.LogInformation("Agent {AgentId}: Attempting to load background audio (ID: {FileId})", _agentState.AgentId, audioUrl);

            try
            {
                AudioFileResult? fileResult = await _audioRepository.GetFileWithMetadataAsync(audioUrl);
                if (fileResult == null || fileResult.Data.IsEmpty)
                {
                    fileResult = await _audioRepository.GetFileWithMetadataAsync(audioUrl, audioUrlOriginRegion);
                    if (fileResult == null || fileResult.Data.IsEmpty)
                    {
                        _logger.LogWarning("Agent {AgentId}: Background audio file not found or is empty (ID: {FileId})", _agentState.AgentId, audioUrl);
                        _agentState.IsBackgroundMusicEnabled = false;
                        _agentState.IsBackgroundMusicLoaded = false;
                        return;
                    }
                }

                // Determine Format and Convert
                ReadOnlyMemory<byte> rawPcmData;
                string? contentType = null;
                if (fileResult.Metadata.TryGetValue("x-amz-meta-filecontenttype", out contentType) && !string.IsNullOrWhiteSpace(contentType))
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
            try
            {
                await _synthesisSemaphore.WaitAsync(externalToken);

                if (string.IsNullOrWhiteSpace(text) || _agentState.TTSService == null)
                {
                    if (markTurnAsCompleteAfterThis)
                    {
                        _speechAudioQueue.CompleteAdding();
                    }

                    _logger.LogWarning("Agent {AgentId}: Text is empty or TTS service is not configured for turn {TurnId} for text '{Text}' with adding complete {IsAddingCompleted}.", _agentState.AgentId, turn.Id, (text.Length > 50 ? text.Substring(0, 50) + "..." : text), markTurnAsCompleteAfterThis);
                    return (false, TimeSpan.Zero);
                }

                // Cancel previous TTS task if running, create a new CTS linked to overall shutdown and external token
                try
                {
                    _currentTtsTaskCTS?.Cancel();
                    _currentTtsTaskCTS?.Dispose();
                }
                catch { }
                _currentTtsTaskCTS = CancellationTokenSource.CreateLinkedTokenSource(_audioSendingCTS.Token, externalToken); // Link to module CTS and external one
                var ttsToken = _currentTtsTaskCTS.Token;

                try
                {
                    var cacheRetrievalLatencyStopwatch = Stopwatch.StartNew();
                    (bool isCacheable, string? cacheGroupId, string? cacheEntryId) = await IsTextCacheable(text);
                    ITTSConfig ttsConfig = _agentState.TTSService.GetCacheableConfig();
                    string cacheKey = TTSCacheKeyGenerator.Generate(text, _agentState.TTSService.GetProviderType(), ttsConfig);

                    if (isCacheable)
                    {                      
                        var cacheResult = await _cacheManager.TryGetAudioAsync(cacheKey, ttsConfig, _agentState.TTSService.GetProviderType(), _agentState.BusinessApp.Id, cacheGroupId, _agentState.CurrentLanguageCode, cacheEntryId, turn.Response.AgentId, ttsToken);
                        cacheRetrievalLatencyStopwatch.Stop();
                        if (cacheResult.IsHit && !cacheResult.AudioData.IsEmpty)
                        {
                            var cachedSegment = new SpeechSegment(Guid.NewGuid().ToString(), text, cacheResult.AudioData, cacheResult.Duration, turn.Id, true, (int)cacheRetrievalLatencyStopwatch.ElapsedMilliseconds);
                            _speechAudioQueue.Add(cachedSegment, _audioSendingCTS.Token);
                            if (markTurnAsCompleteAfterThis)
                            {
                                _speechAudioQueue.CompleteAdding();
                            }

                            _logger.LogDebug("Agent {AgentId}: Returning cached audio for turn {TurnId} with text \"{Text}\" with adding complete {IsAddingCompleted}.", _agentState.AgentId, turn.Id, (text.Length > 50 ? text.Substring(0, 50) + "..." : text), markTurnAsCompleteAfterThis);

                            return (true, cachedSegment.Duration);
                        }
                    }

                    var generationLatencyStopwatch = Stopwatch.StartNew();
                    var (audioData, audioDuration) = await _agentState.TTSService.SynthesizeTextAsync(text, ttsToken, null);
                    generationLatencyStopwatch.Stop();
                    if (ttsToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("Agent {AgentId}: TTS service was cancelled for turn {TurnId} with text \"{Text}\" with adding complete {IsAddingCompleted}.", _agentState.AgentId, turn.Id, (text.Length > 50 ? text.Substring(0, 50) + "..." : text), markTurnAsCompleteAfterThis);

                        if (markTurnAsCompleteAfterThis)
                        {
                            _speechAudioQueue.CompleteAdding();
                        }

                        return (false, TimeSpan.Zero);
                    }

                    if (audioData == null || audioData.Length == 0 || audioDuration == null || audioDuration.Value <= TimeSpan.Zero)
                    {
                        _logger.LogError("Agent {AgentId}: TTS service returned null or empty audio/duration for text: \"{Text}\" with adding complete {IsAddingCompleted}.", _agentState.AgentId, (text.Length > 50 ? text.Substring(0, 50) + "..." : text), markTurnAsCompleteAfterThis);

                        if (markTurnAsCompleteAfterThis)
                        {
                            _speechAudioQueue.CompleteAdding();
                        }

                        return (false, TimeSpan.Zero);
                    }

                    if (isCacheable && audioData.Length != 0)
                    {
                        _logger.LogDebug("Agent {AgentId}: Caching audio for turn {TurnId} with text \"{Text}\".", _agentState.AgentId, turn.Id, (text.Length > 50 ? text.Substring(0, 50) + "..." : text));

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
                            turn.Response.AgentId,
                            CancellationToken.None
                        );
                    }

                    var segment = new SpeechSegment(Guid.NewGuid().ToString(), text, audioData, audioDuration.Value, turn.Id, false, (int)generationLatencyStopwatch.ElapsedMilliseconds);
                    _speechAudioQueue.Add(segment, _audioSendingCTS.Token);
                    if (markTurnAsCompleteAfterThis)
                    {
                        _speechAudioQueue.CompleteAdding();
                    }

                    _logger.LogDebug("Agent {AgentId}: Returning audio for turn {TurnId} with text \"{Text}\" with adding complete {IsAddingCompleted}.", _agentState.AgentId, turn.Id, (text.Length > 50 ? text.Substring(0, 50) + "..." : text), _speechAudioQueue.IsAddingCompleted);

                    return (true, segment.Duration);
                }
                catch (OperationCanceledException) when (ttsToken.IsCancellationRequested || _audioSendingCTS.Token.IsCancellationRequested)
                {
                    _logger.LogWarning("Agent {AgentId}: Cancelled synthesizing speech for text: {Text}", _agentState.AgentId, text.Length > 50 ? text.Substring(0, 50) + "..." : text);
                    return (false, TimeSpan.Zero);
                }
                catch (InvalidOperationException) when (_speechAudioQueue.IsAddingCompleted)
                {
                    _logger.LogWarning("Agent {AgentId}: Could not queue speech segment as queue is completed for turn {TurnId} with text {Text}.", _agentState.AgentId, turn.Id, (text.Length > 50 ? text.Substring(0, 50) + "..." : text));
                    return (false, TimeSpan.Zero);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Agent {AgentId}: Error synthesizing speech for turn {TurnId} with text: {Text}", _agentState.AgentId, turn.Id, text.Length > 50 ? text.Substring(0, 50) + "..." : text);
                    // TODO: Raise error event
                    return (false, TimeSpan.Zero);
                }
            }
            finally
            {
                _synthesisSemaphore.Release();
            }
        }
        public async Task SynthesizeAndPlayBlockingAsync(ConversationTurn turn, string text, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            try
            {
                // 1. Cancel any currently playing speech first (important for blocking calls)
                await CancelCurrentSpeechPlaybackAsync();

                // 2. Synthesize and queue the new speech
                var (success, duration) = await SynthesizeAndQueueSpeechAsync(turn, text, true, cancellationToken);
                if (_currentTtsTaskCTS!.IsCancellationRequested)
                {
                    turn.Response.SpeechCompletedAt = DateTime.UtcNow;
                    TurnUpdate?.Invoke(this, turn);

                    return;
                }

                // 3. Wait for the estimated duration if synthesis was successful
                if (success && duration > TimeSpan.Zero)
                {
                    while (turn.Response.SpeechCompletedAt == null)
                    {
                        // Check for cancellations
                        if (cancellationToken.IsCancellationRequested || _currentTtsTaskCTS.IsCancellationRequested)
                        {
                            break;
                        }

                        // If the queue is somehow dead or the agent state changed unexpectedly
                        if (_agentState.CurrentTurn?.Id != turn.Id)
                        {
                            break;
                        }

                        // Poll every 50ms
                        await Task.Delay(50, cancellationToken);
                    }
                }
                else if (!success)
                {
                    _logger.LogError("Agent {AgentId}: Failed to synthesize speech for turn {TurnId} with blocking message: {Text}", turn.Id, _agentState.AgentId, text.Length > 50 ? text.Substring(0, 50) + "..." : text);
                    // Handle error - maybe log, maybe try fallback?
                }
            }
            catch (OperationCanceledException) { /* Expected */ }
            catch (Exception ex) {
                _logger.LogError(ex, "Agent {AgentId}: Error synthesizing speech for turn {TurnId} with blocking message: {Text}", _agentState.AgentId, turn.Id, text.Length > 50 ? text.Substring(0, 50) + "..." : text);
                // Handle error - maybe log, maybe try fallback?
            }
        }
        public async Task CancelCurrentSpeechPlaybackAsync()
        {
            _logger.LogDebug("Agent {AgentId}: Canceling current speech playback.", _agentState.AgentId);

            try
            {
                _currentTtsTaskCTS?.Cancel();
                _currentTtsTaskCTS?.Dispose();
            }
            catch { }
            
            await (_agentState.TTSService?.StopTextSynthesisAsync() ?? Task.CompletedTask);

            if (!_speechAudioQueue.IsAddingCompleted)
            {
                _speechAudioQueue.CompleteAdding();
            }
            while (_speechAudioQueue.TryTake(out _)) { }        

            _speechAudioQueue = new BlockingCollection<SpeechSegment>(new ConcurrentQueue<SpeechSegment>());

            // Reset local playback state
            _currentSpeechSegmentId = string.Empty;
            _currentSpeechSegmentAudio = ReadOnlyMemory<byte>.Empty;
            _currentSpeechSegmentAudioPosition = 0;
            _currentSpeechSegmentText = string.Empty;
            _isPlaybackPaused = false;
            _playbackPausedAt = null;

            // Reset checks
            _turnStreamingStartedAt = null;
            _totalAudioDurationSent = TimeSpan.Zero;

            OnAudioBufferCleared?.Invoke(this, null);
        }
        public void StopSending()
        {
            _speechAudioQueue.CompleteAdding();
            try
            {
                _audioSendingCTS?.Cancel();
            }
            catch { }
             
            try
            {
                _currentTtsTaskCTS?.Cancel();
            }
            catch { }
        }
        public Task PausePlaybackAsync()
        {
            _logger.LogDebug("Agent {AgentId}: Pausing playback.", _agentState.AgentId);

            if (!_isPlaybackPaused)
            {
                _playbackPausedAt = DateTime.UtcNow;
                _isPlaybackPaused = true;
            }
            return Task.CompletedTask;
        }
        public Task ResumePlaybackAsync()
        {
            _logger.LogDebug("Agent {AgentId}: Resuming playback.", _agentState.AgentId);

            if (_isPlaybackPaused)
            {
                _playbackPausedAt = null;
                _isPlaybackPaused = false;
            }
            return Task.CompletedTask;
        }
        public async Task InterruptCurrentTurnSegment()
        {
            var turnBeingCancelled = _agentState.CurrentTurn;
            if (turnBeingCancelled != null &&
                turnBeingCancelled.Response.Type == ConversationTurnAgentResponseType.Speech &&
                (turnBeingCancelled.Status == ConversationTurnStatus.AgentProcessing || turnBeingCancelled.Status == ConversationTurnStatus.AgentRespondingSpeech)
            ) {
                _logger.LogDebug("Agent {AgentId}: Interrupting current turn segment for turn {TurnId}.", turnBeingCancelled.Id, _agentState.AgentId);

                var activeSegment = turnBeingCancelled.Response.SpokenSegments.Find(s => s.Id == _currentSpeechSegmentId);

                if (activeSegment != null)
                {
                    activeSegment.WasInterrupted = true;

                    if (_playbackPausedAt != null)
                    {
                        activeSegment.FinishedPlayingAt = _playbackPausedAt;
                    }
                }

                if (_playbackPausedAt != null)
                {
                    turnBeingCancelled.Response.SpeechCompletedAt = _playbackPausedAt;
                }

                TurnUpdate?.Invoke(this, turnBeingCancelled);
            }
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
                return (true, autoCacheGroupId, existingQuery.Id);
            }
            else
            {
                var newCacheAudio = new BusinessAppCacheAudio
                {
                    Id = Guid.NewGuid().ToString(), // Generate a new unique ID
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
                                autoCacheGroup.Audios[languageCode].Add(taskResult.Data!);
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
                    if (
                        !_currentSpeechSegmentAudio.IsEmpty &&
                        _currentSpeechSegmentAudioPosition < _currentSpeechSegmentAudio.Length &&
                        !_isPlaybackPaused
                    ) {
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
                                _logger.LogDebug("Agent {AgentId}: Finished playing speech segment {SegmentId}.", _agentState.AgentId, _currentSpeechSegmentId);

                                var segmentDataToUpdate = currentTurn.Response.SpokenSegments.FirstOrDefault(s => s.Id == _currentSpeechSegmentId);
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

                        var newSegmentData = new ConversationTurnSpeechSegmentData
                        {
                            Id = nextSegment.Id,
                            Text = nextSegment.Text,
                            Duration = nextSegment.Duration,
                            StartedPlayingAt = DateTime.UtcNow,
                            IsCacheHit = nextSegment.IsCacheHit,
                            RetrievalLatencyMS = nextSegment.RetrievalLatencyMS
                        };
                        currentTurn.Response.SpokenSegments.Add(newSegmentData);
                        TurnUpdate?.Invoke(this, currentTurn);

                        _currentSpeechSegmentId = nextSegment.Id;
                        _currentSpeechSegmentAudio = nextSegment.AudioData;
                        _currentSpeechSegmentDuration = nextSegment.Duration;
                        _currentSpeechSegmentText = nextSegment.Text;
                        _currentSpeechSegmentAudioPosition = 0;

                        // Immediately process the first chunk of the new segment
                        int firstSpeechChunkSize = Math.Min(FirstBytesPerChunk, _currentSpeechSegmentAudio.Length);
                        if (firstSpeechChunkSize > 0)
                        {
                            var speechChunk = _currentSpeechSegmentAudio.Slice(_currentSpeechSegmentAudioPosition, firstSpeechChunkSize);
                            var backgroundChunk = GetNextBackgroundChunk(firstSpeechChunkSize);
                            chunkToSend = MixAudioChunksWhileApplyingVolumeAndClipping(speechChunk, backgroundChunk);

                            _currentSpeechSegmentAudioPosition += firstSpeechChunkSize;

                            if (_currentSpeechSegmentAudioPosition >= _currentSpeechSegmentAudio.Length)
                            {
                                _logger.LogDebug("Agent {AgentId}: Finished playing speech segment {SegmentId}.", _agentState.AgentId, _currentSpeechSegmentId);

                                var segmentDataToUpdate = currentTurn.Response.SpokenSegments.FirstOrDefault(s => s.Id == _currentSpeechSegmentId);
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
                        var backgroundChunk = GetNextBackgroundChunk(_isFirstBackgroundAudioChunkWithoutSpeech ? FirstBytesPerChunk : BytesPerChunk);
                        if (!backgroundChunk.IsEmpty)
                        {
                            chunkToSend = MixAudioChunksWhileApplyingVolumeAndClipping(ReadOnlyMemory<byte>.Empty, backgroundChunk);
                        }

                        // we sent more chunks first time already now, so next time if its again background, it will just use bytes per chunk
                        _isFirstBackgroundAudioChunkWithoutSpeech = false;
                    }
                    else
                    {
                        // means we have speech, so the next time chunk to send is empty, it will use the first bytes per chunk
                        _isFirstBackgroundAudioChunkWithoutSpeech = true;
                    }

                    // --- Send Chunk and Delay ---
                    if (chunkToSend != null && chunkToSend.Length > 0)
                    {
                        // A. Initialize Start Time on First Packet
                        DateTime? turnStreamingStartedAt = _turnStreamingStartedAt;
                        if (turnStreamingStartedAt == null)
                        {
                            turnStreamingStartedAt = DateTime.UtcNow;
                            _turnStreamingStartedAt = turnStreamingStartedAt;
                        }

                        // B. Send the Audio (Fire & Forget to Socket)
                        AudioChunkGenerated?.Invoke(this, new ConversationAudioGeneratedEventArgs(chunkToSend, _agentState.CurrentClientId));

                        // C. Update Virtual Clock (How much audio duration have we sent?)
                        // Calculation: (Bytes / BytesPerSecond) = Seconds
                        double chunkDurationSec = (double)chunkToSend.Length / (SampleRate * BytesPerSample * Channels);
                        _totalAudioDurationSent = _totalAudioDurationSent.Add(TimeSpan.FromSeconds(chunkDurationSec));

                        // D. CALCULATE BUDGET
                        // How much time has passed in the real world since we started speaking?
                        TimeSpan timeElapsedRealWorld = DateTime.UtcNow - turnStreamingStartedAt.Value;

                        // How much audio is currently "ahead" of the real world? (Buffered on client/network)
                        TimeSpan bufferCurrentSize = _totalAudioDurationSent - timeElapsedRealWorld;

                        // E. THROTTLE DECISION
                        if (bufferCurrentSize.TotalMilliseconds > MaxBufferAheadMs)
                        {
                            // We are too fast! The client has more than MaxBufferAheadMs (e.g., 600ms) of audio.
                            // We must slow down to real-time speed to prevent overflowing the buffer.
                            // We wait exactly the amount needed to bring the buffer back to the target.

                            int delayNeeded = (int)(bufferCurrentSize.TotalMilliseconds - MaxBufferAheadMs);

                            // Safety clamp (don't wait TOO long, or we might block processing of stop signals)
                            //if (delayNeeded > 0)
                            //{
                            //    if (delayNeeded > 100) delayNeeded = 100; // Cap checks at 100ms to remain responsive
                            //    await Task.Delay(delayNeeded, cancellationToken);
                            //}

                            await Task.Delay(delayNeeded, cancellationToken);
                        }
                        else
                        {
                            // BURST MODE:
                            // bufferCurrentSize is LESS than MaxBufferAheadMs.
                            // Do NOT await. Loop immediately to process the next chunk.
                            // This fills the buffer as fast as the CPU/TTS can generate.
                            await Task.Yield();
                        }
                    }
                    else
                    {
                        // No audio (speech or background) to send, wait briefly
                        await Task.Yield();
                    }

                    // Check if current turn playback is complete
                    if (
                        currentTurn.Status == ConversationTurnStatus.AgentRespondingSpeech &&
                        currentTurn.Response.SpeechCompletedAt == null &&
                        (_currentSpeechSegmentAudio.IsEmpty || _currentSpeechSegmentAudioPosition >= _currentSpeechSegmentAudio.Length) &&
                        (_speechAudioQueue.IsAddingCompleted && _speechAudioQueue.Count == 0)
                    )
                    {
                        // We have sent all the data. But the user is still hearing the audio!
                        // We must wait for the "Virtual Buffer" to drain before declaring the turn complete.
                        if (_turnStreamingStartedAt != null)
                        {
                            TimeSpan timeElapsed = DateTime.UtcNow - _turnStreamingStartedAt.Value;
                            TimeSpan remainingBufferToPlay = _totalAudioDurationSent - timeElapsed;

                            if (remainingBufferToPlay > TimeSpan.Zero)
                            {
                                // Wait for the client to finish playing what we sent.
                                // We use a loop with small delays to stay responsive to cancellations.
                                while (remainingBufferToPlay.TotalMilliseconds > 50 && !cancellationToken.IsCancellationRequested)
                                {
                                    await Task.Delay(50, cancellationToken);
                                    // Recalculate
                                    timeElapsed = DateTime.UtcNow - _turnStreamingStartedAt.Value;
                                    remainingBufferToPlay = _totalAudioDurationSent - timeElapsed;
                                }

                                // Wait the final remainder precision
                                if (remainingBufferToPlay.TotalMilliseconds > 0)
                                {
                                    await Task.Delay(remainingBufferToPlay, cancellationToken);
                                }
                            }
                        }

                        _logger.LogInformation("Agent {AgentId}: Turn {TurnId} playback fully completed (Buffer Drained).", _agentState.AgentId, currentTurn.Id);

                        currentTurn.Response.SpeechCompletedAt = DateTime.UtcNow;

                        if (
                            (currentTurn.Type == ConversationTurnType.User || currentTurn.Type == ConversationTurnType.ToolResult) &&
                            currentTurn.Response.LLMStreamingCompletedAt != null &&
                            currentTurn.Response.Type == ConversationTurnAgentResponseType.Speech
                        )
                        {
                            AgentResponsePlaybackComplete?.Invoke(this, currentTurn);
                        }
                        
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // TODO handle the completion of turn
            }
            catch (Exception ex)
            {
                // TODO handle the completion of turn
                _logger.LogError(ex, "Agent {AgentId}: Error in audio sending task.", _agentState.AgentId);
                // TODO: Raise error event

                StartProcessingAudioTask();
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