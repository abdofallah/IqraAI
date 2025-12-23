using IqraCore.Entities.User;

namespace IqraCore.Models.User.MasterUserDataModel
{
    public class UserApiKeyModel
    {
        public UserApiKeyModel(UserApiKey userApiKey)
        {
            Id = userApiKey.Id;
            FriendlyName = userApiKey.FriendlyName;
            DisplayName = userApiKey.DisplayName;
            CreatedUtc = userApiKey.CreatedUtc;
            LastUsedUtc = userApiKey.LastUsedUtc;
            RestrictedToBusinessIds = userApiKey.RestrictedToBusinessIds;
        }

        public string Id { get; set; }

        public string FriendlyName { get; set; }
        public string DisplayName { get; set; }

        public DateTime CreatedUtc { get; set; }

        public DateTime? LastUsedUtc { get; set; }

        public List<long> RestrictedToBusinessIds { get; set; }
    }
}
