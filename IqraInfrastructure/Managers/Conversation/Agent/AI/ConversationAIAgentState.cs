using IqraCore.Entities.Business;
using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.VAD;
using System.Text;
using IqraCore.Entities.Helper.Agent;

namespace IqraInfrastructure.Managers.Conversation.Agent.AI
{
    public class ConversationAIAgentState
    {
        // Configuration & Identification
        public string AgentId { get; internal set; } = string.Empty;
        public CancellationToken MasterCTS { get; internal set; } = CancellationToken.None;
        public ConversationAgentConfiguration? AgentConfiguration { get; internal set; }
        public BusinessApp? BusinessApp { get; internal set; }
        public BusinessAppRoute? CurrentSessionRoute { get; internal set; }
        public BusinessAppAgent? BusinessAppAgent { get; internal set; }
        public AgentConversationTypeENUM CurrentConversationType { get; internal set; }

        // Language
        public string CurrentLanguageCode { get; internal set; } = string.Empty;

        // Agent State
        public bool IsAwaitingLanguageSelection = false;
        public bool HasChoosenLanguage = false;

        // Integration & Service Instances (Managed possibly by handlers, but accessible here)
        public BusinessAppIntegration? STTBusinessIntegrationData { get; internal set; }
        public ISTTService? STTService { get; internal set; }

        public BusinessAppIntegration? TTSBusinessIntegrationData { get; internal set; }
        public ITTSService? TTSService { get; internal set; }

        public BusinessAppIntegration? LLMBusinessIntegrationData { get; internal set; }
        public ILLMService? LLMService { get; internal set; }
        public string LLMBaseSystemPrompt { get; internal set; } = string.Empty;
        public ILLMService? InterruptingLLMService { get; internal set; }

        public bool IsVadEnabled { get; set; } = false;
        public IVadService? VadService { get; internal set; }
        public VadOptions? VadOptions { get; internal set; } // VAD config might be needed by Input/Interruption

        // Runtime State Flags & Variables
        public bool IsInitialized { get; internal set; } = false; // Overall agent init state
        public bool IsResponding { get; set; } = false; // LLM is generating 'response_to_customer'
        public bool IsExecutingSystemTool { get; set; } = false; // LLM is generating 'execute_system_function'
        public bool IsExecutingCustomTool { get; set; } = false; // LLM is generating 'execute_custom_function'
        public bool IsProcessingInterruption { get; set; } = false; // Interruption LLM is running
        public bool IsUserSpeakingVAD { get; set; } = false; // VAD detection state
        public bool IsAcceptingSTTAudio { get; set; } = false; // Should input audio go to STT?
        public bool IsProcessingDTMFAlready { get; set; } = false; // DTMF Handling lock

        // Audio Output State
        public float CurrentAgentVolumeFactor { get; set; } = 1.0f; // Volume for mixing
        public bool IsBackgroundMusicEnabled { get; internal set; } = false;
        public bool IsBackgroundMusicLoaded { get; internal set; } = false;
        public float BackgroundMusicVolume { get; internal set; } = 0.3f; // Default, could be config
        public ReadOnlyMemory<byte> BackgroundAudioData { get; set; } = ReadOnlyMemory<byte>.Empty; // Loaded data

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
            MasterCTS = masterCTS;
        }
    }
}