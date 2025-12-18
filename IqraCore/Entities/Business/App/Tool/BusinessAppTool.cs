using IqraCore.Attributes;
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

        public List<BusinessAppToolScriptExecuteCustomToolNodeReference> ScriptExecuteCustomToolNodeReferences { get; set; } = new List<BusinessAppToolScriptExecuteCustomToolNodeReference>();
        // todo inbound, telephony, web campaign actions references
    }

    public class BusinessAppToolGeneral
    {
        [MultiLanguageProperty]
        public Dictionary<string, string> Name { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, string> ShortDescription { get; set; } = new Dictionary<string, string>();
    }

    public class BusinessAppToolScriptExecuteCustomToolNodeReference
    { 
        public string ScriptId { get; set; }
        public string NodeId { get; set; }
    }
}
