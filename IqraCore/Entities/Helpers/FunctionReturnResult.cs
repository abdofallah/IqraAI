namespace IqraCore.Entities.Helpers
{
    public class FunctionReturnResult<T>
    {
        public bool Success { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public int Code { get; set; } = -1;
        public T? Data { get; set; } = default(T);
    }
}
