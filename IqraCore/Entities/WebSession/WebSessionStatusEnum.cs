namespace IqraCore.Entities.WebSession
{
    public enum WebSessionStatusEnum
    {
        Queued = 0,
        ProcessingQueue = 1,
        ProcessingBackend = 2,
        ProcessedBackend = 3,
        Failed = 4,
        Canceled = 5,
        Expired = 6
    }
}
