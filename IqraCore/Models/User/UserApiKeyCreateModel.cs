using IqraCore.Models.User.GetMasterUserDataModel;

namespace IqraCore.Models.User
{
    public class UserApiKeyCreateModel
    {
        public string RawApiKey { get; set; }
        public UserApiKeyModel CreatedKey { get; set; }
    }
}
