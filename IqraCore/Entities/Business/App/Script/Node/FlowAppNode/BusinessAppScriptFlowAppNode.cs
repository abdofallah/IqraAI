using IqraCore.Attributes;
using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business.App.Agent.Script.Node.FlowAppNode
{
    public class BusinessAppScriptFlowAppNode : BusinessAppScriptNode
    {
        public override BusinessAppAgentScriptNodeTypeENUM NodeType => BusinessAppAgentScriptNodeTypeENUM.ExecuteFlowApp;

        public string AppKey { get; set; } = string.Empty;
        public string ActionKey { get; set; } = string.Empty;
        public string? IntegrationId { get; set; }

        [MultiLanguageProperty]
        public Dictionary<string, string> SpeakingBeforeExecution { get; set; } = new();

        public List<FlowAppNodeInput> Inputs { get; set; } = new();
    }

    public class FlowAppNodeInput
    {
        public string Key { get; set; } = string.Empty; // e.g. "attendee.email"

        // This can be a String, Number, Boolean, or null
        public object? Value { get; set; }

        public bool IsAiGenerated { get; set; }
        public bool IsRedacted { get; set; }
    }
}