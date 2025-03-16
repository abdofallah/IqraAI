export declare class JobQueue {
    private isFlushing;
    private isFlushPending;
    private scheduleId;
    private queue;
    private frameInterval;
    private initialTime;
    queueJob(job: Job): void;
    queueFlush(): void;
    queueFlushSync(): void;
    clearJobs(): void;
    flushJobs(): void;
    flushJobsSync(): void;
    private findInsertionIndex;
    private scheduleJob;
    private cancelScheduleJob;
    private getCurrentTime;
}
export interface Job {
    id: string;
    priority: JOB_PRIORITY;
    cb: () => void;
}
export declare enum JOB_PRIORITY {
    Update = 2,
    RenderEdge = 4,
    RenderNode = 8,
    PRIOR = 1048576
}
