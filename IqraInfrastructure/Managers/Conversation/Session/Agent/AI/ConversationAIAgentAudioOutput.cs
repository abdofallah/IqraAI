using Deepgram.Models.Agent.v2.WebSocket;
using IqraCore.Entities.Business;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Conversation.Turn;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.TTS;
using IqraCore.Interfaces.TTS;
using IqraInfrastructure.Helpers.Audio;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Conversation.Session.Agent.AI.Helpers;
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
        public int SampleRate { get; }
        public int BitsPerSample { get; }

        public SpeechSegment(string id, string text, ReadOnlyMemory<byte> audioData, TimeSpan duration, string turnId, bool isCacheHit, int retrieveLatencyMS, int sampleRate, int bitsPerSample)
        {
            Id = id;
            Text = text;
            AudioData = audioData;
            Duration = duration;
            TurnId = turnId;
            IsCacheHit = isCacheHit;
            RetrievalLatencyMS = retrieveLatencyMS;
            SampleRate = sampleRate;
            BitsPerSample = bitsPerSample;
        }
    }

    public class ConversationAIAgentAudioOutput : IDisposable
    {
        // Events
        public event EventHandler<ConversationAudioGeneratedEventArgs>? AudioChunkGenerated;
        public event EventHandler<ConversationTurn>? AgentResponsePlaybackComplete;
        public event EventHandler<object?>? OnAudioBufferCleared;
        public event EventHandler<ConversationTurn>? TurnUpdate;

        // Dependencies
        private readonly SessionLoggerFactory _sessionLoggerFactory;
        private readonly ILogger<ConversationAIAgentAudioOutput> _logger;
        private readonly ConversationSessionOrchestrator _conversationSession;
        private readonly ConversationAIAgentState _agentState;
        private readonly TTSProviderManager _ttsProviderManager;
        private readonly BusinessAgentAudioRepository _audioRepository;
        private readonly BusinessManager _businessManager;
        private readonly TTSAudioCacheManager _cacheManager;

        // Master Format State (Controlled by Orchestrator)
        private int _masterSampleRate;
        private int _masterBitsPerSample;
        private int _bytesPerSample;
        private int _bytesPerChunk;
        private const int FRAME_DURATION_MS = 20;
        private const int BACKPRESSURE_THRESHOLD_MS = 200;

        public ConversationAIAgentBackgroundAudioProvider? BackgroundAudioProvider { get; private set; }

        // Queues & Tasks
        private BlockingCollection<SpeechSegment> _speechAudioQueue = new(new ConcurrentQueue<SpeechSegment>());
        private Task? _audioSendingTask;
        private CancellationTokenSource _audioSendingCTS = new();

        // Current Playback State
        private SpeechSegment? _currentSpeechSegment = null;
        private ReadOnlyMemory<byte> _currentSpeechSegmentAudio = ReadOnlyMemory<byte>.Empty;
        private int _currentSpeechSegmentAudioPosition = 0;
        private volatile bool _isPlaybackPaused = false;
        private DateTime? _playbackPausedAt = null;

        // TTS Specific Task Management
        private CancellationTokenSource? _currentTtsTaskCTS = null;
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
            _masterSampleRate = _conversationSession._sessionMasterSampleRate;
            _masterBitsPerSample = _conversationSession._sessionMasterBitsPerSample;
            RecalculateFormatDerivedValues();

            await InitializeTTSAsync();
            await LoadBackgroundMusicAsync();

            _logger.LogInformation("AudioOutput module initialized. Master: {Rate}Hz {Bits}bit. Frame: {Frame}ms", _masterSampleRate, _masterBitsPerSample, FRAME_DURATION_MS);
        }
        public async Task UpgradeMasterFormatAndReinitalizeAsync(int newSampleRate, int newBitsPerSample)
        {
            // we wait for the current synthesis task to finish to update the master sample rate
            // to be safe from messing up using the wrong sample rate/bits
            await _synthesisSemaphore.WaitAsync();

            try
            {
                if (_masterSampleRate == newSampleRate &&
                _masterBitsPerSample == newBitsPerSample
            )
                {
                    return;
                }


                _logger.LogInformation("Upgrading Master Audio Format to {Rate}Hz with {Bits} bits...", newSampleRate, newBitsPerSample);

                bool wasPaused = _isPlaybackPaused;
                await PausePlaybackAsync();

                _masterSampleRate = newSampleRate;
                _masterBitsPerSample = newBitsPerSample;
                RecalculateFormatDerivedValues();

                await CancelCurrentSpeechPlaybackAsync();

                await ReInitializeTTSAndBackgroundAudio();

                if (!wasPaused)
                {
                    await ResumePlaybackAsync();
                }
            }
            finally
            {
                _synthesisSemaphore.Release();
            }
        }
        public async Task ReInitializeTTSAndBackgroundAudio()
        {
            await InitializeTTSAsync();
            await LoadBackgroundMusicAsync();
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

            var ttsServiceResult = await _ttsProviderManager.BuildProviderServiceByIntegration(
                _sessionLoggerFactory,
                _agentState.TTSBusinessIntegrationData,
                defaultTTSServiceInfo,
                _masterSampleRate,
                _masterBitsPerSample,
                AudioEncodingTypeEnum.PCM
            );
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

            var actualFormat = _agentState.TTSService.GetCurrentOutputFormat();

            if (actualFormat.SampleRateHz != _masterSampleRate)
            {
                _logger.LogWarning("TTS Provider Initialized at {Actual}Hz, but Master is {Master}Hz. Conversion will occur.", actualFormat.SampleRateHz, _masterSampleRate);
            }

            _logger.LogInformation("Agent {AgentId}: TTS service initialized/re-initialized.", _agentState.AgentId);
        }
        private async Task LoadBackgroundMusicAsync()
        {
            if (_agentState.BusinessAppAgent?.Settings?.BackgroundAudioS3StorageLink == null)
            {
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
                        _agentState.IsBackgroundMusicLoaded = false;
                        return;
                    }
                }

                AudioEncodingTypeEnum inputEncoding = AudioEncodingTypeEnum.MPEG; // Default guess
                if (fileResult.Metadata.TryGetValue("x-amz-meta-filecontenttype", out var contentType))
                {
                    if (contentType == "audio/wav") inputEncoding = AudioEncodingTypeEnum.WAV;
                }

                var (convertedAudio, _) = AudioConversationHelper.Convert(
                    fileResult.Data.ToArray(),
                    new TTSProviderAvailableAudioFormat { Encoding = inputEncoding, SampleRateHz = 0, BitsPerSample = 0 },
                    new AudioRequestDetails
                    {
                        RequestedEncoding = AudioEncodingTypeEnum.PCM,
                        RequestedSampleRateHz = _masterSampleRate,
                        RequestedBitsPerSample = _masterBitsPerSample
                    },
                    false
                );

                if (convertedAudio == null || convertedAudio.Length == 0)
                {
                    _logger.LogError("Failed to convert background audio to Master PCM format.");
                    BackgroundAudioProvider = null;
                    return;
                }


                BackgroundAudioProvider = new ConversationAIAgentBackgroundAudioProvider(
                    _agentState.BusinessAppAgent.Settings.BackgroundAudioVolume ?? 100,
                    convertedAudio,
                    _masterSampleRate,
                    _masterBitsPerSample
                );

                _agentState.IsBackgroundMusicLoaded = true;

                _conversationSession.AudioEngine?.SetBackgroundSource(_agentState.AgentId, BackgroundAudioProvider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentId}: Error loading or converting background audio (ID: {FileId})", _agentState.AgentId, audioUrl);
                _agentState.IsBackgroundMusicLoaded = false;
            }
        }
        private void RecalculateFormatDerivedValues()
        {
            _bytesPerSample = _masterBitsPerSample / 8;
            _bytesPerChunk = (_masterSampleRate * _bytesPerSample * FRAME_DURATION_MS) / 1000;
        }

        // Management
        public void StartProcessingAudioTask()
        {
            if (_audioSendingTask != null) return;

            _audioSendingTask = Task.Run(() => ProcessAudioProducerLoopAsync(_audioSendingCTS.Token), _audioSendingCTS.Token);
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
                            var cachedSegment = new SpeechSegment(ObjectId.GenerateNewId().ToString(), text, cacheResult.AudioData, cacheResult.Duration, turn.Id, true, (int)cacheRetrievalLatencyStopwatch.ElapsedMilliseconds, _masterSampleRate, _masterBitsPerSample);
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

                    var segment = new SpeechSegment(ObjectId.GenerateNewId().ToString(), text, audioData, audioDuration.Value, turn.Id, false, (int)generationLatencyStopwatch.ElapsedMilliseconds, _masterSampleRate, _masterBitsPerSample);
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
                    await Task.Delay(duration, cancellationToken);

                    if (_currentTtsTaskCTS.IsCancellationRequested) return;

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
            _currentSpeechSegment = null;
            _currentSpeechSegmentAudio = ReadOnlyMemory<byte>.Empty;
            _currentSpeechSegmentAudioPosition = 0;
            _isPlaybackPaused = false;
            _playbackPausedAt = null;

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
            if (
                _currentSpeechSegment.HasValue &&
                turnBeingCancelled != null &&
                turnBeingCancelled.Response.Type == ConversationTurnAgentResponseType.Speech &&
                (turnBeingCancelled.Status == ConversationTurnStatus.AgentProcessing || turnBeingCancelled.Status == ConversationTurnStatus.AgentRespondingSpeech)
            ) {
                _logger.LogDebug("Agent {AgentId}: Interrupting current turn segment for turn {TurnId}.", turnBeingCancelled.Id, _agentState.AgentId);

                var activeSegment = turnBeingCancelled.Response.SpokenSegments.Find(s => s.Id == _currentSpeechSegment.Value.Id);

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

        // Processing Task
        private async Task ProcessAudioProducerLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Check Mixer Health (Backpressure)
                    // If the mixer has more than 300ms of our audio, we wait.
                    // This keeps the "Future Audio" buffer small, so interruptions are snappy.
                    if (_conversationSession.AudioEngine != null)
                    {
                        // Poll until buffer drops below threshold
                        while (
                            _conversationSession.AudioEngine.GetQueueDurationMs(_agentState.AgentId) > BACKPRESSURE_THRESHOLD_MS &&
                            !cancellationToken.IsCancellationRequested &&
                            !_isPlaybackPaused
                        ) {
                            await Task.Delay(20, cancellationToken);
                        }
                    }

                    var currentTurn = _agentState.CurrentTurn;
                    if (currentTurn == null)
                    {
                        await Task.Delay(20, cancellationToken);
                        continue;
                    }

                    byte[]? chunkToSend = null;

                    // Check if we have active speech in the buffer
                    if (
                        !_currentSpeechSegmentAudio.IsEmpty &&
                        _currentSpeechSegmentAudioPosition < _currentSpeechSegmentAudio.Length &&
                        !_isPlaybackPaused
                    ) {
                        int remaining = _currentSpeechSegmentAudio.Length - _currentSpeechSegmentAudioPosition;
                        int size = Math.Min(_bytesPerChunk, remaining);

                        if (size > 0)
                        {
                            chunkToSend = _currentSpeechSegmentAudio.Slice(_currentSpeechSegmentAudioPosition, size).ToArray();
                            _currentSpeechSegmentAudioPosition += size;

                            // Mark Segment Complete in Data
                            if (_currentSpeechSegmentAudioPosition >= _currentSpeechSegmentAudio.Length)
                            {
                                var segData = currentTurn.Response.SpokenSegments.FirstOrDefault(s => s.Id == _currentSpeechSegment!.Value.Id);
                                if (segData != null)
                                {
                                    segData.FinishedPlayingAt = DateTime.UtcNow;
                                    TurnUpdate?.Invoke(this, currentTurn);
                                }
                            }
                        }
                    }

                    // If buffer empty, try get next segment
                    if (chunkToSend == null && !_isPlaybackPaused && _speechAudioQueue.TryTake(out var nextSegment))
                    {
                        // Verify turn match
                        if (currentTurn.Id != nextSegment.TurnId)
                        {
                            // Discard old turn audio
                            continue;
                        }

                        // Register Segment
                        var newSegData = new ConversationTurnSpeechSegmentData
                        {
                            Id = nextSegment.Id,
                            Text = nextSegment.Text,
                            Duration = nextSegment.Duration,
                            StartedPlayingAt = DateTime.UtcNow,
                            IsCacheHit = nextSegment.IsCacheHit,
                            RetrievalLatencyMS = nextSegment.RetrievalLatencyMS
                        };
                        currentTurn.Response.SpokenSegments.Add(newSegData);
                        TurnUpdate?.Invoke(this, currentTurn);

                        // Load into Local Buffer
                        _currentSpeechSegment = nextSegment;
                        _currentSpeechSegmentAudio = nextSegment.AudioData;
                        _currentSpeechSegmentAudioPosition = 0;

                        // Immediately process first chunk
                        int size = Math.Min(_bytesPerChunk, _currentSpeechSegmentAudio.Length);
                        if (size > 0)
                        {
                            chunkToSend = _currentSpeechSegmentAudio.Slice(0, size).ToArray();
                            _currentSpeechSegmentAudioPosition += size;
                        }
                    }

                    // Send Chunk (Produce)
                    if (chunkToSend != null && chunkToSend.Length > 0)
                    {
                        AudioChunkGenerated?.Invoke(this, new ConversationAudioGeneratedEventArgs(
                            chunkToSend,
                            _currentSpeechSegment!.Value.SampleRate,
                            _currentSpeechSegment!.Value.BitsPerSample
                        ));

                        await Task.Yield();
                    }
                    else
                    {
                        // Idle (No speech)
                        await Task.Delay(20, cancellationToken);
                    }

                    // Check Turn Completion (Production Finished)
                    if (
                        currentTurn.Status == ConversationTurnStatus.AgentRespondingSpeech &&
                        currentTurn.Response.SpeechCompletedAt == null &&
                        (_currentSpeechSegmentAudio.IsEmpty || _currentSpeechSegmentAudioPosition >= _currentSpeechSegmentAudio.Length) &&
                        _speechAudioQueue.IsAddingCompleted &&
                        _speechAudioQueue.Count == 0
                    )
                    {
                        if (_conversationSession.AudioEngine != null)
                        {
                            // Wait until the mixer has less than 1 frame (20ms) left.
                            // We use 40ms as a safe threshold to avoid race conditions causing infinite waits.
                            while (_conversationSession.AudioEngine.GetQueueDurationMs(_agentState.AgentId) > 40 && !cancellationToken.IsCancellationRequested)
                            {
                                await Task.Delay(20, cancellationToken);
                            }
                        }

                        _logger.LogInformation("Agent {AgentId}: Finished producing audio for turn {TurnId}.", _agentState.AgentId, currentTurn.Id);

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
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Agent Audio Producer Loop");
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