using IqraCore.Interfaces.Node;
using IqraInfrastructure.HostedServices.Call.Outbound;

namespace IqraInfrastructure.Managers.Node.Monitors
{
    public class ProxyWorkloadMonitor : INodeWorkloadMonitor
    {
        private OutboundCallProcessorService? _processor;

        public ProxyWorkloadMonitor() { }

        public void SetupDependencies(OutboundCallProcessorService processor)
        {
            _processor = processor;
        }

        public async Task<int> GetActiveWorkloadCountAsync()
        {
            var currentMarkedCallCount = _processor?.CurrentMarkedCount ?? 0;
            return currentMarkedCallCount;
        }
    }
}