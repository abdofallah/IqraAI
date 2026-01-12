namespace IqraCore.Entities.Node.Enum
{
    public enum NodeRuntimeStatus
    {
        /// <summary>
        /// Application is booting up, running startup checks.
        /// </summary>
        Starting = 0,

        /// <summary>
        /// Normal operation. Accepting new work.
        /// </summary>
        Running = 1,

        /// <summary>
        /// "Soft Stop". The node is alive but rejecting new work (e.g. Incoming Calls).
        /// Existing work continues. Useful for temporary pauses without killing the container.
        /// </summary>
        Maintenance = 2,

        /// <summary>
        /// "Hard Stop Sequence". The node is rejecting new work and waiting for existing work to finish.
        /// Once workload reaches 0, the process will terminate (exit).
        /// </summary>
        Draining = 3,

        /// <summary>
        /// Application is shutting down immediately.
        /// </summary>
        Stopped = 4
    }
}