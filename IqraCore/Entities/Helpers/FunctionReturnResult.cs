namespace IqraCore.Entities.Helpers
{
    public class FunctionReturnResult<T> : FunctionReturnResult
    {
        public T? Data { get; set; } = default(T); // todo set as internal set

        public FunctionReturnResult<T> SetSuccessResult(T? data)
        {
            this.Success = true;
            this.Data = data;

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
        public bool Success { get; set; } = false; // todo set as internal set
        public string? Message { get; set; } = null; // todo set as internal set
        public string? Code { get; set; } = null; // todo set as internal set

        public FunctionReturnResult SetSuccessResult()
        {
            this.Success = true;

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
