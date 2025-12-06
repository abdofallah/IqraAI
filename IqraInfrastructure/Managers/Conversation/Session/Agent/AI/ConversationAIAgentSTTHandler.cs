using IqraCore.Entities.Helper.Audio;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.STT;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Conversation.Session.Agent.AI
{
    public class ConversationAIAgentSTTHandler : IDisposable
    {
        public event Action<string, bool>? TranscriptionReceived;

        private readonly ILogger<ConversationAIAgentSTTHandler> _logger;
        private readonly ConversationAIAgentState _agentState;
        private readonly STTProviderManager _sttProviderManager;
        private readonly BusinessManager _businessManager;

        public ConversationAIAgentSTTHandler(
            ILoggerFactory loggerFactory,
            ConversationAIAgentState agentState,
            STTProviderManager sttProviderManager,
            BusinessManager businessManager
        )
        {
            _logger = loggerFactory.CreateLogger<ConversationAIAgentSTTHandler>();
            _agentState = agentState;
            _sttProviderManager = sttProviderManager;
            _businessManager = businessManager;
        }

        // Initalize
        public async Task InitializeAsync()
        {
            if (_agentState.BusinessAppAgent == null || string.IsNullOrEmpty(_agentState.CurrentLanguageCode))
            {
                _logger.LogError("Agent {AgentId}: Cannot initialize STT Handler - BusinessAppAgent or LanguageCode missing.", _agentState.AgentId);
                throw new InvalidOperationException("STT Handler requires BusinessAppAgent and LanguageCode in state.");
            }

            var defaultSTTService = _agentState.BusinessAppAgent.Integrations.STT[_agentState.CurrentLanguageCode][0]; // Assuming existence check happened before

            var sttBusinessIntegrationDataResult = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(_agentState.BusinessApp.Id, defaultSTTService.Id);
            if (!sttBusinessIntegrationDataResult.Success || sttBusinessIntegrationDataResult.Data == null)
            {
                _logger.LogError("Agent {AgentId}: Business app STT integration {IntegrationId} not found", _agentState.AgentId, defaultSTTService.Id);
                // TODO: Raise error?
                throw new InvalidOperationException($"Business app STT integration {defaultSTTService.Id} not found");
            }
            _agentState.STTBusinessIntegrationData = sttBusinessIntegrationDataResult.Data;

            var sttServiceResult = await _sttProviderManager.BuildProviderServiceByIntegration(
                _agentState.STTBusinessIntegrationData,
                defaultSTTService,
                // hardcoded values for now
                16000,
                32,
                AudioEncodingTypeEnum.PCM
            );

            if (!sttServiceResult.Success || sttServiceResult.Data == null)
            {
                _logger.LogError("Agent {AgentId}: Failed to build STT service with error: {ErrorMessage}", _agentState.AgentId, sttServiceResult.Message);
                // TODO: Raise error?
                throw new InvalidOperationException($"Failed to build STT service: {sttServiceResult.Message}");
            }

            // Unsubscribe from old service if exists
            DisposeCurrentService();

            // Set new service
            _agentState.STTService = sttServiceResult.Data;
            _agentState.STTService.TranscriptionResultReceived += OnTranscriptionResultReceived;
            _agentState.STTService.OnRecoginizingRecieved += OnRecognizingReceived;

            var initSttServiceResult = await _agentState.STTService.Initialize();
            if (!initSttServiceResult.Success)
            {
                _logger.LogError("Agent {AgentId}: Failed to initialize STT service with error: {ErrorMessage}", _agentState.AgentId, initSttServiceResult.Message);
                // TODO: Raise error?
                throw new InvalidOperationException($"Failed to initialize STT service: [{initSttServiceResult.Code}] {initSttServiceResult.Message}");
            }

            _logger.LogInformation("STT Handler initialized for Agent {AgentId} with language {Language}.", _agentState.AgentId, _agentState.CurrentLanguageCode);
        }
        public async Task ReInitializeForLanguageAsync()
        {
            _logger.LogInformation("Agent {AgentId}: Re-initializing STT Handler.", _agentState.AgentId);

            _agentState.STTService?.StopTranscription();
            await InitializeAsync();
        }

        // Management
        public void StartTranscription()
        {
            _agentState.STTService?.StartTranscription();
            _logger.LogDebug("Agent {AgentId}: STT Transcription explicitly started.", _agentState.AgentId);
        }
        public void StopTranscription()
        {
            _agentState.STTService?.StopTranscription();
            _logger.LogDebug("Agent {AgentId}: STT Transcription explicitly stopped.", _agentState.AgentId);
        }

        // Event Handlers
        private void OnRecognizingReceived(object? sender, string text)
        {
            _agentState.IsSTTRecognizing = true;

            if (!string.IsNullOrWhiteSpace(text))
            {
                if (TranscriptionReceived != null)
                {
                    try
                    {
                        TranscriptionReceived.Invoke(text, false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Agent {AgentId}: Error invoking TranscriptionReceived event handler.", _agentState.AgentId);
                    }
                }
            }
        }
        private async void OnTranscriptionResultReceived(object? sender, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            if (TranscriptionReceived != null)
            {
                try
                {
                    TranscriptionReceived.Invoke(text, true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Agent {AgentId}: Error invoking TranscriptionReceived event handler.", _agentState.AgentId);
                    // TODO: Raise main error event?
                    // todo check why it happened so we can use fallback stt
                }
            }
        }

        // Disposal
        private void DisposeCurrentService()
        {
            if (_agentState.STTService != null)
            {
                _logger.LogDebug("Disposing existing STT service for Agent {AgentId}.", _agentState.AgentId);
                try
                {
                    _agentState.STTService.TranscriptionResultReceived -= OnTranscriptionResultReceived;
                    _agentState.STTService.OnRecoginizingRecieved -= OnRecognizingReceived;
                    _agentState.STTService.StopTranscription(); // Ensure stopped
                                                                // Dispose if the service implements IDisposable
                    (_agentState.STTService as IDisposable)?.Dispose();
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Agent {AgentId}: Exception disposing current STT service.", _agentState.AgentId); }
                _agentState.STTService = null;
            }
        }
        public void Dispose()
        {
            DisposeCurrentService();
            _logger.LogDebug("STT Handler disposed for Agent {AgentId}.", _agentState.AgentId);
        }
    }
}