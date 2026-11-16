using IqraCore.Entities.Business;
using IqraCore.Entities.Helper;

namespace IqraCore.Models.User.Business.App.Tool
{
    public class BusinessAppToolModel
    {
        public BusinessAppToolModel(BusinessAppTool data)
        {
            Id = data.Id;
            General = data.General;
            Configuration = new BusinessAppToolConfigurationModel(data.Configuration);
            Response = data.Response;
            Audio = new BusinessAppToolAudioModel(data.Audio);
        }

        public string Id { get; set; }
        public BusinessAppToolGeneral General { get; set; }
        public BusinessAppToolConfigurationModel Configuration { get; set; }
        public DictionaryStringEnumValue<string, HttpStatusEnum, BusinessAppToolResponse> Response { get; set; }
        public BusinessAppToolAudioModel Audio { get; set; }
    }
}
