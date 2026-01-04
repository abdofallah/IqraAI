using IqraCore.Entities.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using System.Text.Json;

namespace IqraCore.Interfaces.FlowApp
{
    public interface IFlowAction
    {
        /// <summary>
        /// Unique identifier for the action (e.g., "bookMeeting"). 
        /// Must be unique within the App.
        /// </summary>
        string ActionKey { get; }

        /// <summary>
        /// Human-readable name (e.g., "Book a Meeting").
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Short description for the UI tooltip.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Whether the action requires authentication/integration credentials.
        /// If false, the script engine will allow execution without an Integration ID.
        /// Default should be true for most apps.
        /// </summary>
        bool RequiresIntegration => true;

        /// <summary>
        /// Returns the raw JSON Schema defining the input fields, validation, and layout.
        /// This is used by the Frontend to render the form.
        /// Compiled at build time automatically from ActionKey.json
        /// </summary>
        string GetInputSchemaJson() => string.Empty;

        /// <summary>
        /// Defines the possible exit paths for this action node (Success, Error, Conflict, etc.).
        /// </summary>
        IReadOnlyList<ActionOutputPort> GetOutputPorts();

        /// <summary>
        /// Executes the action logic.
        /// </summary>
        /// <param name="resolvedInput">The input arguments, with all Scriban templates already resolved to final values.</param>
        /// <param name="integration">The business integration containing credentials (e.g., Decrypted API Keys). Null if action does not require authentication/is public.</param>
        Task<ActionExecutionResult> ExecuteAsync(JsonElement resolvedInput, BusinessAppIntegrationDecryptedModel? integration);
    }
}
