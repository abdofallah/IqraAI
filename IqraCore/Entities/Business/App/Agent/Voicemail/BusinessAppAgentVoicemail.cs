using IqraCore.Attributes;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentVoicemail
    {
        public bool IsEnabled { get; set; } = false;
        public int InitialCheckDelayMS { get; set; } = 1000;

        public int MLCheckDurationMS { get; set; } = 1000;
        public int MaxMLCheckTries { get; set; } = 2;

        public int VoiceMailMessageVADSilenceThresholdMS { get; set; } = 1000;
        public int VoiceMailMessageVADMaxSpeechDurationMS { get; set; } = 4000;

        public bool OnVoiceMailMessageDetectVerifySTTAndLLM { get; set; } = false;
        public BusinessAppAgentIntegrationData? TranscribeVoiceMessageSTT { get; set; } = null;
        public BusinessAppAgentIntegrationData? VerifyVoiceMessageLLM { get; set; } = null;

        public bool StopSpeakingAgentAfterXMlCheckSuccess { get; set; } = true;
        public bool StopSpeakingAgentAfterVadSilence { get; set; } = false;
        public bool StopSpeakingAgentAfterLLMConfirm { get; set; } = false;
        public int StopSpeakingAgentDelayAfterMatchMS { get; set; } = 1000;

        public bool EndOrLeaveMessageAfterXMLCheckSuccess { get; set; } = true;
        public bool EndOrLeaveMessageAfterVadSilence { get; set; } = false;
        public bool EndOrLeaveMessageAfterLLMConfirm { get; set; } = false;
        public int EndOrLeaveMessageDelayAfterMatchMS { get; set; } = 1000;

        // IF END CALL
        public bool EndCallOnDetect { get; set; } = true;

        // IF LEAVE MESSAGE
        public bool LeaveMessageOnDetect { get; set; } = false;
        [MultiLanguageProperty]
        public Dictionary<string, string>? MessageToLeave { get; set; } = null;
        public int WaitXMSAfterLeavingMessageToEndCall { get; set; } = 1000;
    }
}
