namespace IqraCore.Entities.Conversation.Enum
{
    public enum ConversationSessionEndType
    {
        NotSet = 0,
        InitalizeError = 1,
        ExpiredWaitingForInitalize = 2,
        UserDeclinedOrBusy = 3, // for outbound only
        UserEndedCall = 4,
        AgentEndedCall = 5,
        MidSessionFailure = 6,
        UserSilenceTimeoutReached = 7,
        MaxConversationDurationReached = 8,
        VoicemailDetected = 9 // for outbound only
    }
}
