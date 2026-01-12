using IqraCore.Entities.Server.Metrics.Hardware;

namespace IqraCore.Interfaces.Server
{
    public interface IHardwareMonitor : IDisposable
    {
        /// <summary>
        /// Gets the latest hardware metrics.
        /// Should be called periodically.
        /// </summary>
        /// <returns>Current hardware metrics.</returns>
        HardwareMetrics GetMetrics();

        /// <summary>
        /// Initializes any necessary components or performs initial readings.
        /// Should be called once before the first GetMetrics call.
        /// </summary>
        Task InitializeAsync();
    }
}
