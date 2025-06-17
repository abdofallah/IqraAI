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
        public bool IsEncrypted { get; set; } = false;
        public string? SaveEncryptedToVariable { get; set; } = null;
    }

    // Event Args
    public class DTMFSessionEventArgs : EventArgs
    {
        public string NodeId { get; }
        public string CollectedDigits { get; }
        public DTMFSessionEndReason Reason { get; }
        public string? ClientId { get; }

        public DTMFSessionEventArgs(string nodeId, string digits, DTMFSessionEndReason reason, string? clientId)
        {
            NodeId = nodeId;
            CollectedDigits = digits;
            Reason = reason;
            ClientId = clientId;
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

        private string? _sessionStartedByClientId;

        private Timer? _maxDurationTimer;
        private Timer? _interDigitTimer;

        // Events
        public event EventHandler<DTMFSessionEventArgs>? SessionEnded;

        public ConversationAIAgentDTMFSessionManager(ILoggerFactory loggerFactory, ConversationAIAgentState agentState)
        {
            _logger = loggerFactory.CreateLogger<ConversationAIAgentDTMFSessionManager>();
            _agentState = agentState;
        }

        public DTMFSessionConfig? ActiveSessionConfig => _activeSessionConfig;
        public bool IsSessionActive => _isSessionActive;
        public string? ActiveSessionNodeId => _activeSessionConfig?.AssociatedNodeId;

        public bool StartSession(DTMFSessionConfig config, string? clientId = null)
        {
            if (_isSessionActive)
            {
                _logger.LogWarning("Agent {AgentId}: Cannot start new DTMF session for Node {NewNodeId}, another session for Node {OldNodeId} is already active.",
                    _agentState.AgentId, config.AssociatedNodeId, _activeSessionConfig?.AssociatedNodeId ?? "Unknown");
                return false;
            }

            _sessionStartedByClientId = clientId;
            _activeSessionConfig = config;
            _digitBuffer.Clear();
            _isSessionActive = true;
            _waitingForStartChar = config.StartChar.HasValue;

            // Start Timers
            if (_activeSessionConfig.MaxSessionDurationSeconds > 0)
            {
                _maxDurationTimer = new Timer(OnMaxDurationTimeout, null, TimeSpan.FromSeconds(_activeSessionConfig.MaxSessionDurationSeconds), Timeout.InfiniteTimeSpan);
            }
            if (!_waitingForStartChar)
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

        public void PauseSession()
        {
            if (!_isSessionActive) return;
            _interDigitTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void ResumeSession()
        {
            if (!_isSessionActive) return;
            ResetInterDigitTimer();

        }

        public void CancelSession(string reason = "Externally Cancelled")
        {
            if (!_isSessionActive) return;
            EndSession(DTMFSessionEndReason.Cancelled);
        }

        private void ResetInterDigitTimer()
        {
            if (_activeSessionConfig != null)
            {
                _interDigitTimer?.Dispose();
            }

            if (_activeSessionConfig.InterDigitTimeoutSeconds > 0)
            {
                _interDigitTimer = new Timer(OnInterDigitTimeout, null, TimeSpan.FromSeconds(_activeSessionConfig.InterDigitTimeoutSeconds), Timeout.InfiniteTimeSpan);
            }
        }

        private void OnInterDigitTimeout(object? state)
        {
            if (!_isSessionActive) return;
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

            if (reason != DTMFSessionEndReason.TimeoutInterDigit)
            {
                CleanupTimers();
                _isSessionActive = false;
                _activeSessionConfig = null;
            }

            try
            {
                SessionEnded?.Invoke(this, new DTMFSessionEventArgs(nodeId, collectedDigits, reason, _sessionStartedByClientId));
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