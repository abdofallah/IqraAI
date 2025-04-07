using IqraCore.Interfaces.VAD;
using IqraCore.Interfaces.Conversation; // For ISTTService if needed directly
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace IqraInfrastructure.Managers.Conversation.Modules
{
    public class ConversationAIAgentAudioInput : IDisposable
    {
        private readonly ILogger<ConversationAIAgentAudioInput> _logger;
        private readonly ConversationAIAgentState _agentState;
        private readonly BlockingCollection<byte[]> _audioQueue = new();
        private Task? _audioProcessingTask;
        private CancellationTokenSource? _moduleCTS; // Linked to the main agent CTS

        // Dependencies if needed directly (alternative to accessing via _agentState)
        // private readonly ISTTService _sttService;
        // private readonly IVadService _vadService;

        public ConversationAIAgentAudioInput(ILoggerFactory loggerFactory, ConversationAIAgentState agentState)
        {
            _logger = loggerFactory.CreateLogger<ConversationAIAgentAudioInput>();
            _agentState = agentState;
        }

        public Task InitializeAsync(CancellationToken agentCTS)
        {
            _moduleCTS = CancellationTokenSource.CreateLinkedTokenSource(agentCTS);
            _audioProcessingTask = Task.Run(() => ProcessAudioQueueAsync(_moduleCTS.Token), _moduleCTS.Token);
            _logger.LogInformation("AudioInput module initialized for Agent {AgentId}.", _agentState.AgentId);
            return Task.CompletedTask;
        }

        public void ProcessAudioChunk(byte[] audioData, CancellationToken cancellationToken)
        {
            if (!_moduleCTS?.IsCancellationRequested ?? true)
            {
                try
                {
                    // Use a combined token if the external one should also be able to cancel adding
                    var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _moduleCTS.Token).Token;
                    _audioQueue.Add(audioData, combinedToken);
                }
                catch (OperationCanceledException) { /* Expected */ }
                catch (InvalidOperationException) { /* Queue completed */ }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Agent {AgentId}: Error adding audio chunk to queue.", _agentState.AgentId);
                }
            }
        }

        private async Task ProcessAudioQueueAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Agent {AgentId}: Audio processing task started.", _agentState.AgentId);
            try
            {
                foreach (var audioData in _audioQueue.GetConsumingEnumerable(cancellationToken))
                {
                    // --- Move logic from original ProcessAudioQueueAsync here ---
                    // Access _agentState.IsAcceptingSTTAudio, _agentState.STTService, _agentState.VadService

                    if (_agentState.IsAcceptingSTTAudio && _agentState.STTService != null)
                    {
                        try
                        {
                            // Assuming STT Handler provides a method or STTService is directly usable
                            // Option 1: Access service via state
                            _agentState.STTService.WriteTranscriptionAudioData(audioData);
                            // Option 2: Call method on STT Handler (if passed in constructor)
                            // _sttHandler.WriteAudioData(audioData);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Agent {AgentId}: Error writing audio data to STT service.", _agentState.AgentId);
                            // TODO: Raise error event via orchestrator?
                        }
                    }
                    if (_agentState.VadService != null)
                    {
                        try
                        {
                            // Assuming VAD Handler provides method or VADService is directly usable
                            // Option 1: Access service via state
                            _agentState.VadService.ProcessAudio(audioData.AsMemory());
                            // Option 2: Call method on VAD/Interruption Handler
                            // _interruptionManager.ProcessVadAudio(audioData.AsMemory());
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Agent {AgentId}: Error processing audio in VAD service.", _agentState.AgentId);
                            // TODO: Raise error event via orchestrator?
                        }
                    }
                    // --- End of moved logic ---
                }
            }
            catch (OperationCanceledException) { /* Expected */ }
            catch (InvalidOperationException) { /* Expected on shutdown */ }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentId}: Unhandled error in audio processing task.", _agentState.AgentId);
                // TODO: Raise error event via orchestrator?
            }
            finally
            {
                _logger.LogInformation("Agent {AgentId}: Audio processing task finished.", _agentState.AgentId);
            }
        }

        public void StopProcessing() // Called during shutdown
        {
            _audioQueue.CompleteAdding();
            // Cancellation should handle stopping the task loop
        }

        public void Dispose()
        {
            StopProcessing();
            _moduleCTS?.Dispose();
            _audioQueue?.Dispose();
            _audioProcessingTask?.Wait(TimeSpan.FromSeconds(2)); // Optional wait
            _logger.LogDebug("AudioInput module disposed for Agent {AgentId}.", _agentState.AgentId);
        }
    }
}