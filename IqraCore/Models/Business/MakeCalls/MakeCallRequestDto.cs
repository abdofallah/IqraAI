namespace IqraCore.Models.Business.MakeCalls
{
    public class MakeCallRequestDto
    {
        public MakeCallGeneralDto General { get; set; }
        public MakeCallNumberDetailsDto NumberDetails { get; set; }
        public MakeCallConfigurationDto Configuration { get; set; }
        public MakeCallAgentSettingsDto AgentSettings { get; set; }
        public MakeCallActionsDto Actions { get; set; }
    }

    public class MakeCallGeneralDto
    {
        public string Identifier { get; set; }
        public string Description { get; set; }
    }

    public class MakeCallNumberDetailsDto
    {
        public string Type { get; set; } // "single" or "bulk"
        public string? FromNumberId { get; set; }
        public string? ToNumber { get; set; } // Only for single calls
    }

    public class MakeCallConfigurationDto
    {
        public MakeCallScheduleDto Schedule { get; set; }
        public MakeCallRetryConfigDto RetryDecline { get; set; }
        public MakeCallRetryConfigDto RetryMiss { get; set; }
        public MakeCallTimeoutsDto Timeouts { get; set; }
    }

    public class MakeCallScheduleDto
    {
        public string Type { get; set; } // "now" or "later"
        public string? DateTimeUTC { get; set; } // ISO 8601 format string
    }

    // Simplified Retry Config
    public class MakeCallRetryConfigDto
    {
        public bool Enabled { get; set; }
        public int? Count { get; set; }
        public int? Delay { get; set; }
        public string? Unit { get; set; } // "seconds", "minutes", "hours", "days"
    }

    public class MakeCallTimeoutsDto
    {
        public int NotifyOnSilenceMS { get; set; }
        public int EndOnSilenceMS { get; set; }
        public int MaxCallTimeS { get; set; }
    }

    public class MakeCallAgentSettingsDto
    {
        public string? AgentId { get; set; }
        public string? ScriptId { get; set; }
        public string? LanguageCode { get; set; } // Added Language
        public MakeCallInterruptionDto Interruption { get; set; }
        public string? Timezone { get; set; }
        public MakeCallAgentContextDto Context { get; set; }
    }

    public class MakeCallInterruptionDto
    {
        public int Type { get; set; } // Corresponds to AgentInterruptionTypeENUM
        public bool? UseInterruptedResponseInNextTurn { get; set; } // For TurnByTurn
        public int? VadDurationMS { get; set; } // For VAD
        public bool? UseAgentLLM { get; set; } // For AI
        public string? LlmIntegrationId { get; set; } // For AI if UseAgentLLM is false
    }

    public class MakeCallAgentContextDto
    {
        public bool IncludeFromNumber { get; set; }
        public bool IncludeToNumber { get; set; }
    }

    public class MakeCallActionsDto
    {
        public MakeCallActionToolConfigDto? Declined { get; set; }
        public MakeCallActionToolConfigDto? Missed { get; set; } // Renamed from Misscall for consistency
        public MakeCallActionToolConfigDto? Answered { get; set; } // Renamed from PickedUp
        public MakeCallActionToolConfigDto? Ended { get; set; }
    }

    public class MakeCallActionToolConfigDto
    {
        public string ToolId { get; set; } = string.Empty;
        public Dictionary<string, object> Arguments { get; set; } // Assuming frontend sends simple key-value pairs
    }
}
