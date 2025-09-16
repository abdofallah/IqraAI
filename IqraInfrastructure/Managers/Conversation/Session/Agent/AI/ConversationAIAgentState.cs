using IqraCore.Entities.Business;
using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Conversation.Context;
using IqraCore.Entities.Conversation.Turn;
using IqraCore.Interfaces.AI;
using IqraInfrastructure.Managers.VAD.Silero;
using System.Text;

namespace IqraInfrastructure.Managers.Conversation.Session.Agent.AI
{
    public class ConversationAIAgentState
    {
        // Configuration & Identification
        public string AgentId { get; set; } = string.Empty;
        public CancellationToken MasterCancellationToken { get; set; } = CancellationToken.None;
        public ConversationAgentConfiguration? AgentConfiguration { get; set; }
        public BusinessApp? BusinessApp { get; set; }
        public ConversationSessionContext? CurrentSessionContext { get; set; }
        public BusinessAppAgent? BusinessAppAgent { get; set; }

        // Language
        public string CurrentLanguageCode { get; set; } = string.Empty;
        public bool IsAwaitingLanguageSelectionInput { get; set; } = false;

        // Integration & Service Instances (Managed possibly by handlers, but accessible here)
        public BusinessAppIntegration? STTBusinessIntegrationData { get; set; }
        public ISTTService? STTService { get; set; }
        public bool IsSTTRecognizing { get; set; } = false;

        public BusinessAppIntegration? TTSBusinessIntegrationData { get; set; }
        public ITTSService? TTSService { get; set; }

        public BusinessAppIntegration? LLMBusinessIntegrationData { get; set; }
        public ILLMService? LLMService { get; set; }
        public string LLMBaseSystemPrompt { get; set; } = string.Empty;

        // Vad Related
        public SileroVadCore? SileroVadCore { get; set; } = null;

        // Turn Management
        public ConversationTurn? CurrentTurn { get; set; } = null;


        // Runtime State Flags & Variables
        public bool IsInitialized { get; set; } = false;
        public bool IsResponding { get; set; } = false;
        public bool IsAcceptingSTTAudio { get; set; } = false;

        // Audio Output State
        public float CurrentAgentVolumeFactor { get; set; } = 1.0f; // Volume for mixing
        public bool IsBackgroundMusicEnabled { get; set; } = false;
        public bool IsBackgroundMusicLoaded { get; set; } = false;
        public float BackgroundMusicVolume { get; set; } = 0.3f; // Default, could be config
        public ReadOnlyMemory<byte> BackgroundAudioData { get; set; } = ReadOnlyMemory<byte>.Empty; // Loaded data
        public TimeSpan AudioDurationLeftToPlay { get; set; } = TimeSpan.Zero;

        // Client Context
        public string? CurrentClientId { get; set; }
        public Dictionary<string, string> ClientContextMap { get; } = new(); // Example, adjust if needed

        // Timings / Durations (Managed by relevant handlers)
        public DateTime? CurrentResponseDurationSpeakingStarted { get; set; }
        public TimeSpan CurrentResponseDuration { get; set; } = TimeSpan.Zero;

        public ConversationAIAgentState(string agentId, CancellationToken masterCTS)
        {
            AgentId = agentId;
            MasterCancellationToken = masterCTS;
        }
    }
}