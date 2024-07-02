using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessUser
    {
        [BsonId]
        public string Email { get; set; }

        public string Password { get; set; }

        public bool CanUserLogin { get; set; }

        public string? UserNotAllowedToLoginHTMLNotice { get; set; }

        public BusinessUserPermission Permission { get; set; }

        public BusinessUserWhiteLabel WhiteLabel { get; set; }
    }
}
