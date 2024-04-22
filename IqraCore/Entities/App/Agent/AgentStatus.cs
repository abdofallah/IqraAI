namespace IqraCore.Entities.App.Agent
{
    public enum AgentStatus
    {
        Created,

        Initialized,
        InitializedFailed,

        CheckingForRingCommand,
        CheckingForVoiceBeginCommand,

        OnCall,
        Idle,

        Error
    }
}
