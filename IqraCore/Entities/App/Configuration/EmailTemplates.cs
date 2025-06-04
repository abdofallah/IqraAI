using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.App.Configuration
{
    [BsonIgnoreExtraElements]
    public class EmailTemplates
    {
        public EmailTemplateData VerifyEmailTemplate { get; set; } = new EmailTemplateData();
        public EmailTemplateData WelcomeUserTemplate { get; set; } = new EmailTemplateData();
        public EmailTemplateData ResetPasswordTemplate { get; set; } = new EmailTemplateData();
    }

    public class EmailTemplateData
    {
        public string Subject { get; set; } = "Iqra AI";
        public string Body { get; set; } = "Hey! You should not have gotten this email. Notify us if you see this. Thank you.";
    }
}
