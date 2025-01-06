using IqraCore.Entities.Interfaces;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IqraCore.Entities.STT
{
    public class STTProviderData
    {
        [BsonId]
        public InterfaceSTTProviderEnum Id { get; set; } = InterfaceSTTProviderEnum.Unknown;
        public DateTime? DisabledAt { get; set; } = null;
        public List<STTProviderModelData> Models { get; set; } = new List<STTProviderModelData>();
        public string IntegrationId { get; set; }
        public List<STTProviderUserIntegrationFieldData> UserIntegrationFields { get; set; } = new List<STTProviderUserIntegrationFieldData>();
    }
}
