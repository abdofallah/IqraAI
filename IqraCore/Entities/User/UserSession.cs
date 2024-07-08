namespace IqraCore.Entities.User
{
    public class UserSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Token { get; set; } = Guid.NewGuid().ToString();
    }
}
