namespace IqraCore.Entities.FlowApp.Dto
{
    public class FlowActionDefinitionDto
    {
        public string ActionKey { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool RequiresIntegration { get; set; } = true;

        // Raw JSON Schema string for the frontend form renderer
        public string InputSchemaJson { get; set; } = "{}";

        public List<ActionOutputPort> OutputPorts { get; set; } = new();
    }
}
