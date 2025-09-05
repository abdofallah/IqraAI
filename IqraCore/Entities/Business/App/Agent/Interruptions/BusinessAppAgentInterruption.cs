namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentInterruption
    {
        public BusinessAppAgentInterruptionTurnEnd TurnEnd { get; set; } = new BusinessAppAgentInterruptionTurnEnd();
        public bool UseTurnByTurnMode { get; set; } = false;
        public bool? IncludeInterruptedSpeechInTurnByTurnMode { get; set; } = null;
        public BusinessAppAgentInterruptionPauseTrigger?  PauseTrigger { get; set; } = null;
        public BusinessAppAgentInterruptionVerification? Verification { get; set; } = null;
    }
}
