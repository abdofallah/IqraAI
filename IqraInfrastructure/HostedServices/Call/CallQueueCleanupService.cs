using IqraCore.Entities.Server;
using IqraInfrastructure.Repositories.Call;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.HostedServices.Call
{
    public class CallQueueCleanupService : BackgroundService
    {
        private readonly ILogger<CallQueueCleanupService> _logger;
        private readonly InboundCallQueueRepository _callQueueRepository;

        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

        public CallQueueCleanupService(
            ILogger<CallQueueCleanupService> logger,
            InboundCallQueueRepository callQueueRepository
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
                    // Check for expired queued calls
                    int expiredQueues = await _callQueueRepository.CleanupExpiredInboundCallQueues();

                    // Check for orphaned queued calls (processing but no session even after enough time?)
                    int orphanedQueues = await _callQueueRepository.CleanupInboundOrphanedCallQueues(DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(5)));

                    // TODO we should log these 3 if they are not 0 for analytics as this should only happen when there seems to be a major error
                    // for now just log
                    if (expiredQueues > 0 || orphanedQueues > 0)
                    {
                        _logger.LogWarning($"Cleanedup:\nExpiredQueues: {expiredQueues}, OrphanedQueues: {orphanedQueues}");
                    }
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