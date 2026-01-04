using System.Text.Json.Serialization;

namespace IqraCore.Entities.FlowApp.Dto
{
    public class FlowAppDefinitionDto
    {
        public string AppKey { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string? IntegrationType { get; set; } = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<FlowActionDefinitionDto> Actions { get; set; } // do not initialize with new, FlowAppDefWithPermissionModel causes conflit/duplicate

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<FlowDataFetcherDefinitionDto> Fetchers { get; set; } // do not initialize with new, FlowAppDefWithPermissionModel causes conflit/duplicate
    }
}
