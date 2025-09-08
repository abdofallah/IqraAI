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

        // Initalize
        public void InitializeAsync(CancellationToken agentCTS)
        {
            _moduleCTS = CancellationTokenSource.CreateLinkedTokenSource(agentCTS);
            _audioProcessingTask = Task.Run(() => ProcessAudioQueueAsync(_moduleCTS.Token), _moduleCTS.Token);
        }

        // Management
        public void QueueAudioChunk(byte[] audioData, CancellationToken cancellationToken)
        {
            if (!_moduleCTS?.IsCancellationRequested ?? true)
            {
                try
                {
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
        public void StopProcessing()
        {
            _audioQueue.CompleteAdding();
        }

        // Background Task
        private async Task ProcessAudioQueueAsync(CancellationToken cancellationToken)
        {
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

        // Disposal
        public void Dispose()
        {
            StopProcessing();
            _moduleCTS?.Dispose();
            _audioQueue?.Dispose();
            _audioProcessingTask?.Wait(TimeSpan.FromSeconds(2)); // Optional wait
        }
    }
}