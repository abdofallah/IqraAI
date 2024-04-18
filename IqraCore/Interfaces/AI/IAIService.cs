using System;
using System.Threading.Tasks;

namespace IqraCore.Interfaces.AI
{
    public interface IAIService
    {
        Task<string> ProcessInputAsync(string input, CancellationToken cancellationToken);
        void SetModel(string model);
        void SetTemperature(decimal temperature);
        void SetMaxTokens(int maxTokens);
        void SetSystemPrompt(string systemPrompt);
        void SetInitialMessage(string initialMessage);
        event EventHandler<string> MessageStreamed;
    }
}