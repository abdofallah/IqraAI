using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace IqraInfrastructure.Managers.Conversation.Session.Agent.AI
{
    public class ConversationAIAgentAudioInput : IDisposable
    {
        private readonly ILogger<ConversationAIAgentAudioInput> _logger;
        private readonly ConversationAIAgentState _agentState;
        private readonly BlockingCollection<byte[]> _audioQueue = new();
        private Task? _audioProcessingTask;
        private CancellationTokenSource? _moduleCTS;

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
                    if (_agentState.IsAcceptingSTTAudio && _agentState.STTService != null)
                    {
                        try
                        {
                            _agentState.STTService.WriteTranscriptionAudioData(audioData);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Agent {AgentId}: Error writing audio data to STT service.", _agentState.AgentId);
                            // TODO: Raise error event via orchestrator?
                        }
                    }
                    if (_agentState.IsVadEnabled && _agentState.VadService != null)
                    {
                        try
                        {
                            _agentState.VadService.ProcessAudio(audioData);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Agent {AgentId}: Error processing audio in VAD service.", _agentState.AgentId);
                            // TODO: Raise error event via orchestrator?
                        }
                    }
                }
            }
            catch (OperationCanceledException) { /* Expected */ }
            catch (InvalidOperationException) { /* Expected on shutdown */ }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentId}: Unhandled error in audio processing task.", _agentState.AgentId);
                // TODO: Raise error event via orchestrator?
            }
        }

        public void StopProcessing()
        {
            _audioQueue.CompleteAdding();
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