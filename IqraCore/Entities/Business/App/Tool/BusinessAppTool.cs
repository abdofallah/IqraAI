using IqraCore.Entities.Helper;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace IqraCore.Entities.Business
{
    public class BusinessAppTool
    {
        [BsonId]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public BusinessAppToolGeneral General { get; set; } = new BusinessAppToolGeneral();
        public BusinessAppToolConfiguration Configuration { get; set; } = new BusinessAppToolConfiguration();

        [BsonDictionaryOptions(DictionaryRepresentation.Document)]
        public DictionaryStringEnumValue<string, HttpStatusEnum, BusinessAppToolResponse> Response { get; set; } = new DictionaryStringEnumValue<string, HttpStatusEnum, BusinessAppToolResponse>();
        public BusinessAppToolAudio Audio { get; set; } = new BusinessAppToolAudio();

        // References
        public List<BusinessAppToolScriptExecuteCustomToolNodeReference> ScriptExecuteCustomToolNodeReferences { get; set; } = new List<BusinessAppToolScriptExecuteCustomToolNodeReference>();
        public List<BusinessAppToolInboundRouteReference> InboundRouteReferences { get; set; } = new List<BusinessAppToolInboundRouteReference>();
        public List<BusinessAppToolTelephonyCampaignReference> TelephonyCampaignReferences { get; set; } = new List<BusinessAppToolTelephonyCampaignReference>();
        public List<BusinessAppToolWebCampaignReference> WebCampaignReferences { get; set; } = new List<BusinessAppToolWebCampaignReference>();
    }

    public class BusinessAppToolScriptExecuteCustomToolNodeReference
    { 
        public string ScriptId { get; set; }
        public string NodeId { get; set; }
    }

    public class BusinessAppToolInboundRouteReference
    {
        public string RouteId { get; set; }
        public BusinessAppToolInboundRouteActionType ActionType { get; set; }
    }
    public enum BusinessAppToolInboundRouteActionType
    {
        Ringing = 0,
        CallPicked = 1,
        CallEnded = 2
    }

    public class BusinessAppToolTelephonyCampaignReference
    {
        public string CampaignId { get; set; }
        public BusinessAppToolTelephonyCampaignActionType ActionType { get; set; }
    }
    public enum BusinessAppToolTelephonyCampaignActionType
    {
        CallInitiationFailure = 0,
        CallInitiated = 1,
        CallDeclined = 2,
        CallMissed = 3,
        CallAnswered = 4,
        CallEnded = 5
    }

    public class BusinessAppToolWebCampaignReference
    {
        public string CampaignId { get; set; }
        public BusinessAppToolWebCampaignActionType ActionType { get; set; }
    }
    public enum BusinessAppToolWebCampaignActionType
    {
        ConversationInitiationFailure = 0,
        ConversationInitiated = 1,
        ConversationEnded = 2
    }
}
