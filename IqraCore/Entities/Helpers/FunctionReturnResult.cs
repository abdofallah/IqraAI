namespace IqraCore.Entities.Helpers
{
    public class FunctionReturnResult<T>
    {
        public bool Success { get; set; } = false;
        public string? Message { get; set; } = null;
        public string? Code { get; set; } = null;
        public T? Data { get; set; } = default(T);
    }
}
