using IqraCore.Entities.Business;

namespace IqraCore.Models.User.Business.App.Agent
{
    public class BusinessAppAgentSettingsModel
    {
        public BusinessAppAgentSettingsModel(BusinessAppAgentSettings data)
        {
            // BackgroundAudioUrl filled manually by getting presigned url
            BackgroundAudioVolume = data.BackgroundAudioVolume;
        }

        public string? BackgroundAudioUrl { get; set; } = null;
        public int? BackgroundAudioVolume { get; set; } = null;
    }
}
