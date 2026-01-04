using IqraCore.Entities.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using System.Text.Json;

namespace IqraCore.Interfaces.FlowApp
{
    public interface IFlowDataFetcher
    {
        /// <summary>
        /// Unique Key referenced in JSON Schema via the custom property 'x-fetcher'.
        /// Example: "GetEventTypes" or "GetUsers"
        /// </summary>
        string FetcherKey { get; }

        /// <summary>
        /// Indicates if this fetcher requires a valid integration (API Key) to run.
        /// Default: true. Set to false for public APIs (e.g. Country list).
        /// </summary>
        bool RequiresIntegration => true;


        /// <summary>
        /// Fetches dynamic options from the provider.
        /// </summary>
        /// <param name="integration">The business integration credentials (null if RequiresIntegration is false).</param>
        /// <param name="context">
        /// The current state of the form inputs on the frontend. 
        /// Used for dependent fetchers (e.g. Fetch EventTypes based on selected 'username').
        /// </param>
        /// <returns>A list of options to populate the frontend dropdown.</returns>
        Task<List<DynamicOption>> FetchOptionsAsync(BusinessAppIntegrationDecryptedModel? integration, JsonElement context);
    }
}