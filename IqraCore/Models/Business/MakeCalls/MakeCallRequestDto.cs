using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Agent;
using IqraCore.Entities.Helper.Call.Outbound;

namespace IqraCore.Models.Business.MakeCalls
{
    public class MakeCallRequestDto
    {
        public MakeCallGeneralDto? General { get; set; }
        public MakeCallNumberDetailsDto? NumberDetails { get; set; }
        public MakeCallConfigurationDto? Configuration { get; set; }
        public MakeCallAgentSettingsDto? AgentSettings { get; set; }
        public MakeCallActionsDto? Actions { get; set; }
        public Dictionary<string, string>? DynamicVariables { get; set; }
    }

    public class MakeCallGeneralDto
    {
        public string? Identifier { get; set; }
        public string? Description { get; set; }
    }

    public class MakeCallNumberDetailsDto
    {
        public OutboundCallNumberType? Type { get; set; }
        public string? FromNumberId { get; set; }
        public string? ToNumber { get; set; } // Only for single calls
    }

    public class MakeCallConfigurationDto
    {
        public MakeCallScheduleDto? Schedule { get; set; }
        public MakeCallRetryConfigDto? RetryDecline { get; set; }
        public MakeCallRetryConfigDto? RetryMiss { get; set; }
        public MakeCallTimeoutsDto? Timeouts { get; set; }
    }

    public class MakeCallScheduleDto
    {
        public OutboundCallScheduleType? Type { get; set; }
        public DateTime? DateTimeUTC { get; set; } // UTC
    }

    public class MakeCallRetryConfigDto
    {
        public bool? Enabled { get; set; }

        // if enabled
        public int? Count { get; set; }
        public int? Delay { get; set; }
        public OutboundCallRetryDelayUnitType? Unit { get; set; }
    }

    public class MakeCallTimeoutsDto
    {
        public int? NotifyOnSilenceMS { get; set; }
        public int? EndOnSilenceMS { get; set; }
        public int? MaxCallTimeS { get; set; }
    }

    public class MakeCallAgentSettingsDto
    {
        public string? AgentId { get; set; }
        public string? ScriptId { get; set; }
        public string? LanguageCode { get; set; }
        public List<string>? Timezones { get; set; }
        public bool? IncludeFromNumberInContext { get; set; }
        public bool? IncludeToNumberInContext { get; set; }
        public MakeCallInterruptionDto? Interruption { get; set; }
    }

    public class MakeCallInterruptionDto
    {
        public AgentInterruptionTypeENUM? Type { get; set; }

        // if turn by turn
        public bool? UseInterruptedResponseInNextTurn { get; set; } // For TurnByTurn

        // if vad
        public int? VadDurationMS { get; set; } // For VAD

        // if ai
        public bool? UseAgentLLM { get; set; } // For AI
        public string? LLMIntegrationId { get; set; } // For AI if UseAgentLLM is false
        public Dictionary<string, object>? LLMIntegrationConfigFields { get; set; }
    }

    public class MakeCallActionsDto
    {
        public MakeCallActionToolConfigDto? Declined { get; set; }
        public MakeCallActionToolConfigDto? Missed { get; set; }
        public MakeCallActionToolConfigDto? Answered { get; set; }
        public MakeCallActionToolConfigDto? Ended { get; set; }
    }

    public class MakeCallActionToolConfigDto
    {
        public string? ToolId { get; set; }
        public Dictionary<string, object>? Arguments { get; set; }
    }
}
