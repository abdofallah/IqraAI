namespace IqraCore.Entities.Business
{
    public class BusinessToolTestResult
    {
        // Input Processing Phase
        public bool VariableValidationSuccess { get; set; }
        public string? VariableValidationError { get; set; }
        public Dictionary<string, object?>? ResolvedVariables { get; set; }

        // Request Building Phase
        public string? ResolvedEndpoint { get; set; }
        public string? RequestMethod { get; set; }
        public Dictionary<string, string>? ResolvedHeaders { get; set; }
        public string? ResolvedBody { get; set; }

        // HTTP Execution Phase
        public bool HttpExecutionSuccess { get; set; }
        public string? HttpExecutionError { get; set; }
        public int? ResponseStatusCode { get; set; }
        public string? ResponseStatusText { get; set; }
        public Dictionary<string, string>? ResponseHeaders { get; set; }
        public string? ResponseRawBody { get; set; }
        public bool ResponseBodyTruncated { get; set; }
        public long? ResponseTimeMs { get; set; }

        // JS Processing Phase
        public bool HasResponseProcessor { get; set; }
        public bool JsProcessorSuccess { get; set; }
        public string? JsProcessorError { get; set; }
        public string? JsProcessorResult { get; set; }

        // Static Response Phase
        public bool HasStaticResponse { get; set; }
        public string? StaticResponseText { get; set; }

        // Final Combined Result (what AI would receive)
        public string? FinalAIOutput { get; set; }
    }
}
