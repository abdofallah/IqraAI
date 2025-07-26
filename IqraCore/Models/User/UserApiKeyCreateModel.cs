using IqraCore.Entities.User;

namespace IqraCore.Models.User
{
    public class UserApiKeyCreateModel
    {
        public string RawApiKey { get; set; }
        public UserApiKey CreatedKey { get; set; }
    }
}
