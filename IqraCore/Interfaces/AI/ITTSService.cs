using IqraCore.Entities.Interfaces;

namespace IqraCore.Interfaces.AI
{
    public interface ITTSService
    {
        void Initialize();

        Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken);
        Task StopTextSynthesisAsync();
        string GetProviderFullName();
        InterfaceTTSProviderEnum GetProviderType();
        static InterfaceTTSProviderEnum GetProviderTypeStatic()
        {
            return InterfaceTTSProviderEnum.Unknown;
        }
    }
}