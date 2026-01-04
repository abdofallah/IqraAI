using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;

namespace IqraCore.Interfaces.Integration
{
    public interface IFlowApp
    {
        /// <summary>
        /// Unique system identifier (e.g., "cal_com").
        /// </summary>
        string AppKey { get; }

        /// <summary>
        /// Display name (e.g., "Cal.com").
        /// </summary>
        string Name { get; }

        /// <summary>
        /// URL to the app icon/logo.
        /// </summary>
        string IconUrl { get; }

        /// <summary>
        /// Links this App to a specific Integration Type in the Admin Dashboard 
        /// (e.g., "CalCom"). This tells the UI which credentials to ask for.
        /// Nullable: If null, the app is Public (no API Key needed) and requires no setup in the Admin Dashboard.
        /// </summary>
        string? IntegrationType { get; }

        /// <summary>
        /// The list of available actions this app provides.
        /// </summary>
        IReadOnlyList<IFlowAction> Actions { get; }

        /// <summary>
        /// All dynamic data sources provided by this app for UI dropdowns.
        /// </summary>
        IReadOnlyList<IFlowDataFetcher> DataFetchers { get; }
    }
}
