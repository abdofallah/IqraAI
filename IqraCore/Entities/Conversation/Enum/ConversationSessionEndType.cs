namespace IqraCore.Entities.Conversation.Enum
{
    public enum ConversationSessionEndType
    {
        NotSet = 0,
        InitalizeError = 1,
        ExpiredWaitingForInitalize = 2,
        UserDeclinedOrBusy = 3, // for outbound only
        UserNoAnswer = 4, // for outbound only
        UserEndedCall = 5,
        AgentEndedCall = 6,
        MidSessionFailure = 7,
        UserSilenceTimeoutReached = 8,
        MaxConversationDurationReached = 9,
        VoicemailDetected = 10 // for outbound only
    }
}
