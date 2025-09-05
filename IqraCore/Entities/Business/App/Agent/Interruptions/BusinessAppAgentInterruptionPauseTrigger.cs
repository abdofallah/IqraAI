using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentInterruptionPauseTrigger
    {
        public AgentInterruptionPauseTriggerTypeENUM Type { get; set; } = AgentInterruptionPauseTriggerTypeENUM.VAD;

        // Via VAD
        public int? VadDurationMS { get; set; } = null;

        // Via STT
        public int? WordCount { get; set; } = null;
    }
}