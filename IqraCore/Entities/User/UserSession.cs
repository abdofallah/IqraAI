using MongoDB.Bson;

namespace IqraCore.Entities.User
{
    public class UserSession
    {
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        public string Token { get; set; } = ObjectId.GenerateNewId().ToString();
    }
}
