using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Business
{
    public class BusinessUser
    {
        [BsonId]
        public string Email { get; set; }

        public string Password { get; set; }

        public bool CanUserLogin { get; set; } = true;

        public string? UserNotAllowedToLoginHTMLNotice { get; set; } = null;

        public BusinessUserPermission Permission { get; set; } = new BusinessUserPermission();

        public BusinessUserWhiteLabel WhiteLabel { get; set; } = new BusinessUserWhiteLabel();
    }
}
