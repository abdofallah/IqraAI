namespace IqraCore.Entities.Languages
{
    public class LanguagePromptsData
    {
        public string ConversationWarmupLLMPrompt { get; set; } = "";
        public string ConversationBasePrompt { get; set; } = "";
        public string FailedConversationBasePromptGenerationPrompt { get; set; } = "";

        public string TurnEndVerificationPrompt { get; set; } = "";

        public string InterruptionVerificationPrompt { get; set; } = "";

        public string VoicemailVerificationPrompt { get; set; } = "";

        public string RagQueryClassifierPrompt { get; set; } = "";
        public string RagQueryRefinementPrompt { get; set; } = "";

        public string PostAnalaysisSummaryGenerationPrompt { get; set; } = "";
        public string PostAnalaysisTagsClassificationPrompt { get; set; } = "";
        public string PostAnalaysisDataExtractionPrompt { get; set; } = "";
    }
}
