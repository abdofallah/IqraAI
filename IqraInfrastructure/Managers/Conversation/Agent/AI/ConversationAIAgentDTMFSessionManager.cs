using Microsoft.Extensions.Logging;
using System.Text;

namespace IqraInfrastructure.Managers.Conversation.Agent.AI
{
    public class DTMFSessionConfig
    {
        public int MaxLength { get; set; } = 10;
        public int MaxSessionDurationSeconds { get; set; } = 30;
        public int InterDigitTimeoutSeconds { get; set; } = 5;
        public char? TerminatorChar { get; set; } = null;
        public char? StartChar { get; set; } = null;
        public string AssociatedNodeId { get; set; } = string.Empty;
    }

    // Event Args
    public class DTMFSessionEventArgs : EventArgs
    {
        public string NodeId { get; }
        public string CollectedDigits { get; }
        public DTMFSessionEndReason Reason { get; }

        public DTMFSessionEventArgs(string nodeId, string digits, DTMFSessionEndReason reason)
        {
            NodeId = nodeId;
            CollectedDigits = digits;
            Reason = reason;
        }
    }

    public enum DTMFSessionEndReason
    {
        CompletedTerminator,
        CompletedMaxLength,
        TimeoutInterDigit,
        TimeoutMaxDuration,
        Cancelled,
        Error
    }


    public class ConversationAIAgentDTMFSessionManager : IDisposable
    {
        private readonly ILogger<ConversationAIAgentDTMFSessionManager> _logger;
        private readonly ConversationAIAgentState _agentState;

        private DTMFSessionConfig? _activeSessionConfig;
        private StringBuilder _digitBuffer = new StringBuilder();
        private bool _isSessionActive = false;
        private bool _waitingForStartChar = false;
        private bool _disposed = false;

        private Timer? _maxDurationTimer;
        private Timer? _interDigitTimer;

        // Events
        public event EventHandler<DTMFSessionEventArgs>? SessionEnded;

        public ConversationAIAgentDTMFSessionManager(ILoggerFactory loggerFactory, ConversationAIAgentState agentState)
        {
            _logger = loggerFactory.CreateLogger<ConversationAIAgentDTMFSessionManager>();
            _agentState = agentState;
        }

        public bool IsSessionActive => _isSessionActive;
        public string? ActiveSessionNodeId => _activeSessionConfig?.AssociatedNodeId;

        public bool StartSession(DTMFSessionConfig config)
        {
            if (_isSessionActive)
            {
                _logger.LogWarning("Agent {AgentId}: Cannot start new DTMF session for Node {NewNodeId}, another session for Node {OldNodeId} is already active.",
                    _agentState.AgentId, config.AssociatedNodeId, _activeSessionConfig?.AssociatedNodeId ?? "Unknown");
                return false;
            }

            
            _activeSessionConfig = config;
            _digitBuffer.Clear();
            _isSessionActive = true;
            _waitingForStartChar = config.StartChar.HasValue;

            // Start Timers
            if (config.MaxSessionDurationSeconds > 0)
            {
                _maxDurationTimer = new Timer(OnMaxDurationTimeout, null, TimeSpan.FromSeconds(config.MaxSessionDurationSeconds), Timeout.InfiniteTimeSpan);
            }

            if (!_waitingForStartChar && config.InterDigitTimeoutSeconds > 0)
            {
                ResetInterDigitTimer();
            }

            return true;
        }

        public void ProcessDigit(string digit)
        {
            if (!_isSessionActive || _activeSessionConfig == null || digit.Length != 1)
            {
                return;
            }

            char digitChar = digit[0];

            // Stop inter-digit timer while processing
            _interDigitTimer?.Change(Timeout.Infinite, Timeout.Infinite);

            // Check for Start Character if required
            if (_waitingForStartChar)
            {
                if (digitChar == _activeSessionConfig.StartChar)
                {
                    _waitingForStartChar = false;
                    ResetInterDigitTimer();
                    return;
                }
                else
                {
                    ResetInterDigitTimer();
                    return;
                }
            }

            // Check for Terminator Character
            if (_activeSessionConfig.TerminatorChar.HasValue && digitChar == _activeSessionConfig.TerminatorChar)
            {
                EndSession(DTMFSessionEndReason.CompletedTerminator);
                return;
            }

            _digitBuffer.Append(digitChar);
            
            // Check for Max Length
            if (_digitBuffer.Length >= _activeSessionConfig.MaxLength)
            {
                EndSession(DTMFSessionEndReason.CompletedMaxLength);
                return;
            }

            // Reset inter-digit timer after valid digit processed
            ResetInterDigitTimer();
        }

        public void CancelSession(string reason = "Externally Cancelled")
        {
            if (!_isSessionActive) return;
            EndSession(DTMFSessionEndReason.Cancelled);
        }

        private void ResetInterDigitTimer()
        {
            if (_activeSessionConfig != null && _activeSessionConfig.InterDigitTimeoutSeconds > 0)
            {
                _interDigitTimer?.Dispose(); // Dispose previous instance
                _interDigitTimer = new Timer(OnInterDigitTimeout, null, TimeSpan.FromSeconds(_activeSessionConfig.InterDigitTimeoutSeconds), Timeout.InfiniteTimeSpan);
            }
        }

        private void OnInterDigitTimeout(object? state)
        {
            if (!_isSessionActive) return; // Should not happen if timer is active, but safety check
            EndSession(DTMFSessionEndReason.TimeoutInterDigit);
        }

        private void OnMaxDurationTimeout(object? state)
        {
            if (!_isSessionActive) return;
            EndSession(DTMFSessionEndReason.TimeoutMaxDuration);
        }

        private void EndSession(DTMFSessionEndReason reason)
        {
            if (!_isSessionActive) return;

            string nodeId = _activeSessionConfig?.AssociatedNodeId ?? "Unknown";
            string collectedDigits = _digitBuffer.ToString();

            // Stop timers and clear state *before* raising event
            CleanupTimers();
            _isSessionActive = false;
            _activeSessionConfig = null;
            // Don't clear buffer immediately, event handler needs it

            try
            {
                SessionEnded?.Invoke(this, new DTMFSessionEventArgs(nodeId, collectedDigits, reason));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentId}: Error invoking SessionEnded event handler for Node {NodeId}", _agentState.AgentId, nodeId);
            }
            finally
            {
                _digitBuffer.Clear();
            }
        }

        private void CleanupTimers()
        {
            _interDigitTimer?.Dispose();
            _interDigitTimer = null;
            _maxDurationTimer?.Dispose();
            _maxDurationTimer = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                CancelSession("Manager Disposed"); // Ensure session ends cleanly
                CleanupTimers();
                SessionEnded = null; // Remove event listeners
            }

            _disposed = true;
        }
    }
}