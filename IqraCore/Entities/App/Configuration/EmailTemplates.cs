using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.App.Configuration
{
    [BsonIgnoreExtraElements]
    public class EmailTemplates
    {
        public EmailTemplateData VerifyEmailTemplate { get; set; }
        public EmailTemplateData WelcomeUserTemplate { get; set; }
        public EmailTemplateData ResetPasswordTemplate { get; set; }
    }

    public class EmailTemplateData
    {
        public string Subject { get; set; }
        public string Body { get; set; }
    }
}
