using IqraCore.Entities.Helpers;

namespace IqraCore.Entities.FlowApp
{
    public class ActionExecutionResult : FunctionReturnResult<object?>
    {
        /// <summary>
        /// The specific port key the script engine should follow after execution.
        /// Defaults to "default" or "success" if not specified.
        /// </summary>
        public string OutputPortKey { get; set; } = "default";

        /// <summary>
        /// Helper to create a successful result pointing to a specific port.
        /// </summary>
        public static ActionExecutionResult SuccessPort(string portKey, object? data = null)
        {
            return new ActionExecutionResult
            {
                Success = true,
                OutputPortKey = portKey,
                Data = data
            };
        }

        /// <summary>
        /// Helper to create a failure result (usually stops flow or goes to Error port).
        /// </summary>
        public new static ActionExecutionResult Failure(string code, string message)
        {
            return new ActionExecutionResult
            {
                Success = false,
                Code = code,
                Message = message,
                OutputPortKey = "error" // Standard convention
            };
        }
    }
}
