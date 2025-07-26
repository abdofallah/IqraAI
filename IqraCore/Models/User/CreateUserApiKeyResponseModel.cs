namespace IqraCore.Models.User
{
    public class CreateUserApiKeyResponseModel
    {
        public string Id { get; set; }
        public string FriendlyName { get; set; }
        public string DisplayName { get; set; }

        // This property holds the raw, unencrypted key for the one-time display in the modal.
        public string ApiKey { get; set; }

        public DateTime Created { get; set; }
        public DateTime? LastUsed { get; set; }
    }
}
