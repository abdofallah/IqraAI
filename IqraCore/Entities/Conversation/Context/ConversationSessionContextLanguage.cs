using IqraCore.Entities.Business;

namespace IqraCore.Entities.Conversation.Context
{
    public class ConversationSessionContextLanguage
    {
        public string DefaultLanguageCode { get; set; } = string.Empty;

        // Inbound only for now
        public bool? MultiLanguageEnabled { get; set; } = null;
        public List<BusinessAppRouteLanguageMultiEnabled>? EnabledMultiLanguages { get; set; } = null;
    }


}
