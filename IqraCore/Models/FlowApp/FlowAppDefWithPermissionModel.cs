using IqraCore.Entities.FlowApp.Dto;

namespace IqraCore.Models.FlowApp
{
    public class FlowAppDefWithPermissionModel : FlowAppDefinitionDto
    {
        public bool IsDisabled { get; set; }
        public string? DisabledReason { get; set; } // Public reason shown to user

        // Override Actions list to include permission model for actions
        public new List<FlowActionDefWithPermissionModel> Actions { get; set; } = new();

        // Override Fetchers list to include permission model for fetchers
        public new List<FlowFetcherDefWithPermissionModel> Fetchers { get; set; } = new();
    }

    public class FlowActionDefWithPermissionModel : FlowActionDefinitionDto
    {
        public bool IsDisabled { get; set; }
        public string? DisabledReason { get; set; }
    }

    public class FlowFetcherDefWithPermissionModel : FlowDataFetcherDefinitionDto
    {
        public bool IsDisabled { get; set; }
        public string? DisabledReason { get; set; }
    }
}