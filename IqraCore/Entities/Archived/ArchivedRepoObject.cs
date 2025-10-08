using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Archived
{
    public class ArchivedRepoObject<T>
    {
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;

        public string ObjectId { get; set; } = string.Empty;
        public T? ObjectData { get; set; } = default(T);
        
        public ArchivedRepoObject(string id, T obj)
        {
            ObjectId = id;
            ObjectData = obj;
        }
    }
}
