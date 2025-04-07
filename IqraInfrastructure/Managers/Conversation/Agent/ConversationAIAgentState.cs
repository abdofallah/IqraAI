using IqraCore.Entities.Business;
using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.VAD;
using IqraCore.Interfaces.Conversation; // Assuming ISTTService, ITTSService are here or accessible
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using IqraCore.Entities.Helper.Agent;

namespace IqraInfrastructure.Managers.Conversation.Modules // Or a more suitable namespace
{
    /// <summary>
    /// Holds shared state accessible by various ConversationAIAgent modules.
    /// </summary>
    public class ConversationAIAgentState
    {
        // Configuration & Identification
        public string AgentId { get; internal set; } = string.Empty;
        public ConversationAgentConfiguration? AgentConfiguration { get; internal set; }
        public BusinessApp? BusinessApp { get; internal set; }
        public BusinessAppRoute? CurrentSessionRoute { get; internal set; }
        public BusinessAppAgent? BusinessAppAgent { get; internal set; }
        public AgentConversationTypeENUM CurrentConversationType { get; internal set; }

        // Language
        public string CurrentLanguageCode { get; internal set; } = string.Empty;

        // Integration & Service Instances (Managed possibly by handlers, but accessible here)
        public BusinessAppIntegration? STTBusinessIntegrationData { get; internal set; }
        public ISTTService? STTService { get; internal set; }

        public BusinessAppIntegration? TTSBusinessIntegrationData { get; internal set; }
        public ITTSService? TTSService { get; internal set; }

        public BusinessAppIntegration? LLMBusinessIntegrationData { get; internal set; }
        public ILLMService? LLMService { get; internal set; }
        public string LLMBaseSystemPrompt { get; internal set; } = string.Empty;
        public ILLMService? InterruptingLLMService { get; internal set; }

        public IVadService? VadService { get; internal set; }
        public VadOptions? VadOptions { get; internal set; } // VAD config might be needed by Input/Interruption

        // Runtime State Flags & Variables
        public bool IsInitialized { get; internal set; } = false; // Overall agent init state
        public volatile bool IsResponding { get; set; } = false; // LLM is generating 'response_to_customer'
        public volatile bool IsExecutingSystemTool { get; set; } = false; // LLM is generating 'execute_system_function'
        public volatile bool IsExecutingCustomTool { get; set; } = false; // LLM is generating 'execute_custom_function'
        public volatile bool IsProcessingInterruption { get; set; } = false; // Interruption LLM is running
        public volatile bool IsUserSpeakingVAD { get; set; } = false; // VAD detection state
        public volatile bool IsAcceptingSTTAudio { get; set; } = false; // Should input audio go to STT?
        public volatile bool IsProcessingDTMFAlready { get; set; } = false; // DTMF Handling lock

        // Audio Output State
        public volatile float CurrentAgentVolumeFactor { get; set; } = 1.0f; // Volume for mixing
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

        // Other Shared Resources / State
        // Add any other fields that were previously private members in ConversationAIAgent
        // and need to be accessed by multiple modules.

        public ConversationAIAgentState(string agentId)
        {
            AgentId = agentId;
        }
    }
}