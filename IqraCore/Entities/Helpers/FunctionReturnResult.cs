namespace IqraCore.Entities.Helpers
{
    public class FunctionReturnResult<T> : FunctionReturnResult
    {
        /// <summary>The actual payload of the response. Null if Success is false.</summary>
        public T? Data { get; set; } = default(T);

        public FunctionReturnResult<T> SetSuccessResult(T? data, string? message = null)
        {
            this.Success = true;
            this.Data = data;
            this.Message = message;

            return this;
        }

        public FunctionReturnResult<T> SetFailureResult(string? code, string? message, T? data = default(T))
        {
            this.Success = false;
            this.Code = code;
            this.Message = message;
            this.Data = data;

            return this;
        }
    }

    public class FunctionReturnResult
    {
        /// <summary>Indicates if the operation was successful.</summary>
        /// <example>true</example>
        public bool Success { get; set; } = false;

        /// <summary>Human-readable message explaining the result or error.</summary>
        /// <example>Entity with id not found</example>
        public string? Message { get; set; } = null;

        /// <summary>Internal application code for error tracking.</summary>
        /// <example>EntityManager:EntityRepoistory:EXCEPTION</example>
        public string? Code { get; set; } = null;

        public FunctionReturnResult SetSuccessResult(string? code = null, string? message = null)
        {
            this.Success = true;
            this.Code = code;
            this.Message = message;

            return this;
        }

        public FunctionReturnResult SetFailureResult(string? code, string? message)
        {
            this.Success = false;
            this.Code = code;
            this.Message = message;

            return this;
        }
    }
}
