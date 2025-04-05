using IqraCore.Entities.Interfaces;

namespace IqraCore.Interfaces.AI
{
    public interface ILLMService
    {
        event EventHandler<object>? MessageStreamed;
        void ClearMessageStreamed();

        event EventHandler MessageStreamedCancelled;
        Task ProcessInputAsync(CancellationToken cancellationToken);
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