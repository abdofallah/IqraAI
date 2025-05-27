namespace IqraCore.Entities.Conversation.Enum
{
    public enum ConversationSessionState
    {
        Created = 0,
        WaitingForPrimaryClient = 1,
        Starting = 2,
        Active = 3,
        Paused = 4,
        Ending = 5,
        Ended = 6,
        Error = 7
    }
}
