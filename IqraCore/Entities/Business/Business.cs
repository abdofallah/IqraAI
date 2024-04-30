using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Business
{
    public class Business
    {
        [BsonId]
        private ObjectId _id {  get; set; }

        public long BusinessId { get; set; }
        public string BusinessName { get; set; }
        public string BusinessPhoneNumber { get; set; }

        public BusinessPromptData BusinessPromptData { get; set; }

        public BusinessAzureSettings? BusinessAzureSettings { get; set; }
        public string BusinessClaudeApiKey { get; set; }
    }
}
