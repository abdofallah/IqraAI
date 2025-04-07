using IqraCore.Interfaces.Conversation; // For ISTTService
using IqraInfrastructure.Managers.STT; // For STTProviderManager
using IqraInfrastructure.Managers.Business; // For BusinessManager (if needed for re-init)
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IqraInfrastructure.Managers.Conversation.Modules
{
    public class ConversationAIAgentSTTHandler : IDisposable
    {
        // Event to notify Orchestrator about transcription results
        public event Func<string, Task>? TranscriptionReceived; // Orchestrator subscribes

        private readonly ILogger<ConversationAIAgentSTTHandler> _logger;
        private readonly ConversationAIAgentState _agentState;
        private readonly STTProviderManager _sttProviderManager;
        private readonly BusinessManager _businessManager; // Needed to get integration details

        public ConversationAIAgentSTTHandler(
            ILoggerFactory loggerFactory,
            ConversationAIAgentState agentState,
            STTProviderManager sttProviderManager,
            BusinessManager businessManager)
        {
            _logger = loggerFactory.CreateLogger<ConversationAIAgentSTTHandler>();
            _agentState = agentState;
            _sttProviderManager = sttProviderManager;
            _businessManager = businessManager;
        }

        public async Task InitializeAsync()
        {
            // --- Move logic from original InitalizeSTTForLangauge here ---
            // Access _agentState for config (_agentState.BusinessAppAgent, _agentState.CurrentLanguageCode, etc.)
            // Use _sttProviderManager to build the service
            // Store the service instance in _agentState.STTService
            // Store integration data in _agentState.STTBusinessIntegrationData
            // Subscribe to STTService events (_sttService.TranscriptionResultReceived += OnTranscriptionResultReceived)

            if (_agentState.BusinessAppAgent == null || string.IsNullOrEmpty(_agentState.CurrentLanguageCode))
            {
                _logger.LogError("Agent {AgentId}: Cannot initialize STT Handler - BusinessAppAgent or LanguageCode missing.", _agentState.AgentId);
                throw new InvalidOperationException("STT Handler requires BusinessAppAgent and LanguageCode in state.");
            }

            var defaultSTTService = _agentState.BusinessAppAgent.Integrations.STT[_agentState.CurrentLanguageCode][0]; // Assuming existence check happened before

            var sttBusinessIntegrationDataResult = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(_agentState.AgentConfiguration!.BusinessId, defaultSTTService.Id);
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
                new Dictionary<string, string> { { "language", _agentState.CurrentLanguageCode } }
            );

            if (!sttServiceResult.Success || sttServiceResult.Data == null)
            {
                _logger.LogError("Agent {AgentId}: Failed to build STT service with error: {ErrorMessage}", _agentState.AgentId, sttServiceResult.Message);
                // TODO: Raise error?
                throw new InvalidOperationException($"Failed to build STT service: {sttServiceResult.Message}");
            }

            // Unsubscribe from old service if exists
            DisposeCurrentService();

            _agentState.STTService = sttServiceResult.Data;
            _agentState.STTService.TranscriptionResultReceived += OnTranscriptionResultReceived;
            _agentState.STTService.OnRecoginizingRecieved += OnRecognizingReceived; // Keep if needed

            _agentState.STTService.Initialize();
            _agentState.STTService.StartTranscription(); // Start immediately? Or based on state?

            _logger.LogInformation("STT Handler initialized for Agent {AgentId} with language {Language}.", _agentState.AgentId, _agentState.CurrentLanguageCode);
            // --- End of moved logic ---
        }

        public async Task ReInitializeForLanguageAsync()
        {
            _logger.LogInformation("Agent {AgentId}: Re-initializing STT Handler for new language.", _agentState.AgentId);
            // Stop existing transcription before re-init
            _agentState.STTService?.StopTranscription();
            await InitializeAsync(); // Re-run init logic with new language in state
        }

        private void OnRecognizingReceived(object? sender, object e)
        {
            // Handle recognizing if needed (e.g., raise a specific event)
            // _logger.LogTrace("Agent {AgentId}: STT recognizing: {Data}", _agentState.AgentId, e?.ToString());
        }

        private async void OnTranscriptionResultReceived(object? sender, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            _logger.LogInformation("Agent {AgentId} received transcription: {Text}", _agentState.AgentId, text);
            if (TranscriptionReceived != null)
            {
                try
                {
                    await TranscriptionReceived.Invoke(text); // Notify Orchestrator
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Agent {AgentId}: Error invoking TranscriptionReceived event handler.", _agentState.AgentId);
                    // TODO: Raise main error event?
                }
            }
        }

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

        // This might not be needed if AudioInput accesses service via state
        // public void WriteAudioData(byte[] data)
        // {
        //     _agentState.STTService?.WriteTranscriptionAudioData(data);
        // }

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