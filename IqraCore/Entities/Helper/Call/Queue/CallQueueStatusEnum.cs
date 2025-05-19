namespace IqraCore.Entities.Helper.Call.Queue
{
    public enum CallQueueStatusEnum
    {
        Queued = 0,
        WaitingForProcessing = 1,
        Processing = 2,
        Processed = 3,
        Failed = 4,
        Canceled = 5,
        Expired = 6
    }
}
