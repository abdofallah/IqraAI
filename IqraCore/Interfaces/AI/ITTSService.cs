using IqraCore.Entities.Interfaces;
using System.Threading.Tasks;

namespace IqraCore.Interfaces.AI
{
    public interface ITTSService
    {
        void Initialize();

        Task<byte[]> SynthesizeTextAsync(string text, CancellationToken cancellationToken);

        string GetProviderFullName();
        InterfaceTTSProviderEnum GetProviderType()
        {
            return InterfaceTTSProviderEnum.Unknown;
        }
    }
}