namespace IqraCore.Entities.Helpers
{
    public class FunctionReturnResult<T> : FunctionReturnResult
    {
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
        public bool Success { get; set; } = false;
        public string? Message { get; set; } = null;
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
