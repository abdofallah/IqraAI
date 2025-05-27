namespace IqraCore.Entities.Helper.Call.Queue
{
    public enum CallQueueStatusEnum
    {
        Queued = 0,
        ProcessingProxy = 1,
        ProcessedProxy = 2,
        ProcessingBackend = 3,
        ProcessedBackend = 4,
        Completed = 5,
        Failed = 6,
        Canceled = 7,
        Expired = 8
    }
}
