using IqraCore.Models.User.MasterUserDataModel;

namespace IqraCore.Models.User
{
    public class UserApiKeyCreateModel
    {
        public string RawApiKey { get; set; }
        public UserApiKeyModel CreatedKey { get; set; }
    }
}
