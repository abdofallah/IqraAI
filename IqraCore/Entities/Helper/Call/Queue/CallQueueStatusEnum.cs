namespace IqraCore.Entities.Helper.Call.Queue
{
    public enum CallQueueStatusEnum
    {
        Queued = 0,
        ProcessingProxy = 1,
        ProcessedProxy = 2,
        ProcessingBackend = 3,
        ProcessedBackend = 4,
        Failed = 5,
        Canceled = 6,
        Expired = 7
    }
}
