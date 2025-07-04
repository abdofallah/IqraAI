using IqraCore.Entities.Business;
using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.VAD;
using System.Text;
using IqraCore.Entities.Helper.Agent;
using IqraCore.Entities.Conversation.Context;

namespace IqraInfrastructure.Managers.Conversation.Session.Agent.AI
{
    public class ConversationAIAgentState
    {
        // Configuration & Identification
        public string AgentId { get; internal set; } = string.Empty;
        public CancellationToken MasterCancellationToken { get; internal set; } = CancellationToken.None;
        public ConversationAgentConfiguration? AgentConfiguration { get; internal set; }
        public BusinessApp? BusinessApp { get; internal set; }
        public ConversationSessionContext? CurrentSessionContext { get; internal set; }
        public BusinessAppAgent? BusinessAppAgent { get; internal set; }
        public AgentInterruptionTypeENUM CurrentConversationType { get; internal set; }

        // Language
        public string CurrentLanguageCode { get; internal set; } = string.Empty;
        public bool IsAwaitingLanguageSelectionInput { get; set; } = false;

        // Integration & Service Instances (Managed possibly by handlers, but accessible here)
        public BusinessAppIntegration? STTBusinessIntegrationData { get; internal set; }
        public ISTTService? STTService { get; internal set; }
        public bool IsSTTRecognizing { get; set; } = false;

        public BusinessAppIntegration? TTSBusinessIntegrationData { get; internal set; }
        public ITTSService? TTSService { get; internal set; }

        public BusinessAppIntegration? LLMBusinessIntegrationData { get; internal set; }
        public ILLMService? LLMService { get; internal set; }
        public string LLMBaseSystemPrompt { get; internal set; } = string.Empty;
        public ILLMService? InterruptingLLMService { get; internal set; }

        public bool IsVadEnabled { get; set; } = false;
        public IVadService? VadService { get; internal set; }
        public VadOptions? VadOptions { get; internal set; } 

        // Runtime State Flags & Variables
        public bool IsInitialized { get; internal set; } = false;
        public bool IsResponding { get; set; } = false;
        public bool IsExecutingSystemTool { get; set; } = false;
        public bool IsRespondingSystemToolRespone { get; set; } = false;
        public bool IsExecutingCustomTool { get; set; } = false;
        public bool IsRespondingCustomToolRespone { get; set; } = false;
        public bool IsUserSpeakingVAD { get; set; } = false;
        public bool IsAcceptingSTTAudio { get; set; } = false;

        // Audio Output State
        public float CurrentAgentVolumeFactor { get; set; } = 1.0f; // Volume for mixing
        public bool IsBackgroundMusicEnabled { get; internal set; } = false;
        public bool IsBackgroundMusicLoaded { get; internal set; } = false;
        public float BackgroundMusicVolume { get; internal set; } = 0.3f; // Default, could be config
        public ReadOnlyMemory<byte> BackgroundAudioData { get; set; } = ReadOnlyMemory<byte>.Empty; // Loaded data
        public TimeSpan AudioDurationLeftToPlay { get; set; } = TimeSpan.Zero;
        public bool IsAudioPlayingPaused { get; set; } = false;

        // Client Context
        public string? CurrentClientId { get; set; }
        public Dictionary<string, string> ClientContextMap { get; } = new(); // Example, adjust if needed

        // Buffers (Potentially managed within specific handlers but accessible if needed)
        public StringBuilder ResponseBuffer { get; } = new StringBuilder();
        public int CurrentResponseBufferRead { get; set; } = 0;
        public StringBuilder InterruptResponseBuffer { get; } = new StringBuilder();

        // Timings / Durations (Managed by relevant handlers)
        public DateTime? CurrentResponseDurationSpeakingStarted { get; set; }
        public TimeSpan CurrentResponseDuration { get; set; } = TimeSpan.Zero;
        public DateTime? UserSpeechStartTime { get; set; } // VAD related timing

        public ConversationAIAgentState(string agentId, CancellationToken masterCTS)
        {

            AgentId = agentId;
            MasterCancellationToken = masterCTS;
        }
    }
}