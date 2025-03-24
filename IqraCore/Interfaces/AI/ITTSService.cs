using IqraCore.Entities.Interfaces;

namespace IqraCore.Interfaces.AI
{
    public interface ITTSService
    {
        void Initialize();

        Task<byte[]> SynthesizeTextAsync(string text, CancellationToken cancellationToken);

        string GetProviderFullName();
        InterfaceTTSProviderEnum GetProviderType();
        static InterfaceTTSProviderEnum GetProviderTypeStatic()
        {
            return InterfaceTTSProviderEnum.Unknown;
        }
    }
}