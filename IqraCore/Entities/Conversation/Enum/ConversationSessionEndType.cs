namespace IqraCore.Entities.Conversation.Enum
{
    public enum ConversationSessionEndType
    {
        NotSet = 0,
        InitalizeError = 1,
        ExpiredWaitingForInitalize = 2,
        UserDeclinedOrBusy = 3, // for outbound only
        UserEndedCall = 2,
        AgentEndedCall = 3,
        MidSessionFailure = 4,
        UserSilenceTimeoutReached = 5,
        MaxConversationDurationReached = 6
    }
}
