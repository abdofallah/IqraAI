namespace IqraCore.Interfaces.Node
{
    public interface INodeWorkloadMonitor
    {
        /// <summary>
        /// Returns the number of active tasks (e.g., Active Calls, Processing Batches).
        /// Used by the Lifecycle Manager to determine if it is safe to shutdown during Draining state.
        /// </summary>
        Task<int> GetActiveWorkloadCountAsync();
    }
}