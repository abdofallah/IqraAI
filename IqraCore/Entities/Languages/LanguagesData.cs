using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Languages
{
    public class LanguagesData
    {
        [BsonId]
        public string Id { get; set; }
    
        public string LocaleName { get; set; }
        public string Name { get; set; }

        public DateTime DisabledAt { get; set; }
    }
}
