namespace IqraCore.Entities.Helper.Call.Queue
{
    public enum CallQueueStatusEnum
    {
        WaitingForQueueing = 0,
        Queued = 1,
        Processing = 2,
        Processed = 3,
        Failed = 4,
        Canceled = 5,
        Expired = 6
    }
}
