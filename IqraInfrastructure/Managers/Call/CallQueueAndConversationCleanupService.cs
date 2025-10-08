using IqraCore.Entities.Server;
using IqraInfrastructure.Repositories.Call;
using IqraInfrastructure.Repositories.Conversation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Call
{
    public class CallQueueAndConversationCleanupService : BackgroundService
    {
        private readonly ILogger<CallQueueAndConversationCleanupService> _logger;
        private readonly InboundCallQueueRepository _callQueueRepository;
        private readonly ConversationStateRepository _conversationStateRepository;
        private readonly BackendAppConfig _serverConfig;

        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

        public CallQueueAndConversationCleanupService(
            ILogger<CallQueueAndConversationCleanupService> logger,
            InboundCallQueueRepository callQueueRepository,
            ConversationStateRepository conversationStateRepository,
            BackendAppConfig serverConfig
        )
        {
            _logger = logger;
            _callQueueRepository = callQueueRepository;
            _conversationStateRepository = conversationStateRepository;
            _serverConfig = serverConfig;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Queue Cleanup Service starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Check for expired queued calls
                    int expiredQueues = await _callQueueRepository.CleanupExpiredInboundCallQueues(_serverConfig.RegionId);

                    // Check for orphaned queued calls (processing but no session even after enough time?)
                    int orphanedQueues = await _callQueueRepository.CleanupInboundOrphanedCallQueues(_serverConfig.RegionId, DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(5)));

                    // Check for expired conversations
                    int expiredConversations = await _conversationStateRepository.CleanupMaxDurationReachedConversationsAsync(_serverConfig.RegionId, _serverConfig.ServerId, DateTime.UtcNow.AddMinutes(5));

                    // todo log to conversation logs repo
                    //.Push(c => c.Logs, new ConversationStateLogEntry() { Level = ConversationStateLogLevelEnum.Critical, Message = "Expected endtime reached for conversation but it wasnt ended, so manually cleaned up" });

                    // TODO we should log these 3 if they are not 0 for analytics as this should only happen when there seems to be a major error
                    // for now just log
                    if (expiredQueues > 0 || orphanedQueues > 0 || expiredConversations > 0)
                    {
                        _logger.LogWarning($"Cleanedup:\nExpiredQueues: {expiredQueues}, OrphanedQueues: {orphanedQueues}, ExpiredConversations: {expiredConversations}");
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