using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.TTS;

namespace IqraCore.Interfaces.AI
{
    public interface ITTSService
    {
        Task<FunctionReturnResult> Initialize();

        Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData);
        Task StopTextSynthesisAsync();
        string GetProviderFullName();
        InterfaceTTSProviderEnum GetProviderType();
        static InterfaceTTSProviderEnum GetProviderTypeStatic()
        {
            return InterfaceTTSProviderEnum.Unknown;
        }
        ITTSConfig GetCacheableConfig();
    }
}