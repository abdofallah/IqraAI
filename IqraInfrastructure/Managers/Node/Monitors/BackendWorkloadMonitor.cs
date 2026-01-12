using IqraCore.Interfaces.Node;
using IqraInfrastructure.Managers.Call.Backend;
using IqraInfrastructure.Managers.WebSession;

namespace IqraInfrastructure.Managers.Node.Monitors
{
    public class BackendWorkloadMonitor : INodeWorkloadMonitor
    {
        private BackendCallProcessorManager? _callProcessor;
        private BackendWebSessionProcessorManager? _webProcessor;

        public BackendWorkloadMonitor() { }

        public void SetupDependencies(
            BackendCallProcessorManager callProcessor,
            BackendWebSessionProcessorManager webProcessor
        )
        {
            _callProcessor = callProcessor;
            _webProcessor = webProcessor;
        }

        public async Task<int> GetActiveWorkloadCountAsync()
        {
            var activeTelephonySessions = _callProcessor?.ActiveSessionCount ?? 0;
            var activeWebSessions = _webProcessor?.ActiveSessionCount ?? 0;

            return activeTelephonySessions + activeWebSessions;
        }
    }
}