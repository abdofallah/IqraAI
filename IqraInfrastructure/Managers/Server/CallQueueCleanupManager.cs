using IqraInfrastructure.Repositories.Telephony;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Server
{
    public class CallQueueCleanupManager : BackgroundService
    {
        private readonly ILogger<CallQueueCleanupManager> _logger;
        private readonly CallQueueRepository _callQueueRepository;

        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(15);
        private readonly TimeSpan _orphanedCallThreshold = TimeSpan.FromMinutes(30);
        private readonly TimeSpan _orphanedSessionThreshold = TimeSpan.FromHours(1);

        public CallQueueCleanupManager(
            ILogger<CallQueueCleanupManager> logger,
            CallQueueRepository callQueueRepository
        )
        {
            _logger = logger;
            _callQueueRepository = callQueueRepository;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Queue Cleanup Service starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Clean up orphaned calls
                    int cleanedCalls = await _callQueueRepository.CleanupOrphanedCallsAsync(_orphanedCallThreshold);
                    if (cleanedCalls > 0)
                    {
                        _logger.LogInformation("Cleaned up {Count} orphaned calls", cleanedCalls);
                    }

                    // Clean up orphaned sessions
                    // TODO
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during queue cleanup");
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }

            _logger.LogInformation("Queue Cleanup Service stopping");
        }
    }
}