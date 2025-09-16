using IqraCore.Entities.Conversation.Turn;

namespace IqraCore.Entities.Conversation.Events
{
    public class ConversationTurnEventArgs : EventArgs
    {
        public ConversationTurn Turn { get; }

        public ConversationTurnEventArgs(ConversationTurn turn)
        {
            Turn = turn;
        }
    }
}