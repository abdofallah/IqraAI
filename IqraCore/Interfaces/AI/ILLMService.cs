using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Interfaces;

namespace IqraCore.Interfaces.AI
{
    public interface ILLMService : IDisposable
    {
        event EventHandler<ConversationAgentEventLLMStreamed>? MessageStreamed;
        void ClearMessageStreamed();

        event EventHandler<ConversationAgentEventLLMStreamCancelled> MessageStreamedCancelled;
        Task ProcessInputAsync(CancellationToken cancellationToken, string? beforeMessageContext = null, string? afterMessageContext = null);
        void SetModel(string model);
        void SetTemperature(decimal temperature);
        void SetMaxTokens(int maxTokens);
        void SetSystemPrompt(string systemPrompt);
        void AddUserMessage(string message);
        void AddAssistantMessage(string message);
        void EditMessage(int index, string message);
        void ClearMessages();
        string GetModel();
        string GetProviderFullName();
        InterfaceLLMProviderEnum GetProviderType();
        static InterfaceLLMProviderEnum GetProviderTypeStatic()
        {
            return InterfaceLLMProviderEnum.Unknown;
        }
    }
}