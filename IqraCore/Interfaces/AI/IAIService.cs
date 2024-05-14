using System;
using System.Threading.Tasks;

namespace IqraCore.Interfaces.AI
{
    public interface IAIService
    {
        Task ProcessInputAsync(string input, CancellationToken cancellationToken);
        void SetModel(string model);
        void SetTemperature(decimal temperature);
        void SetMaxTokens(int maxTokens);
        void SetSystemPrompt(string systemPrompt);
        void SetInitialMessage(string initialMessage);
        void AddUserMessage(string message);
        void AddAssistantMessage(string message);
        event EventHandler<object> MessageStreamed;

        string GetProviderName();
    }
}