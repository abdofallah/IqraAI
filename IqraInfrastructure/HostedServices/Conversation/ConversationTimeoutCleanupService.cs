using IqraCore.Entities.Server;
using IqraInfrastructure.Repositories.Conversation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.HostedServices.Conversation
{
    internal class ConversationTimeoutCleanupService : BackgroundService
    {
        private readonly ILogger<ConversationTimeoutCleanupService> _logger;
        private readonly ConversationStateRepository _conversationStateRepository;
        private readonly BackendAppConfig _serverConfig;

        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

        public ConversationTimeoutCleanupService(
            ILogger<ConversationTimeoutCleanupService> logger,
            ConversationStateRepository conversationStateRepository,
            BackendAppConfig serverConfig
        )
        {
            _logger = logger;
            _conversationStateRepository = conversationStateRepository;
            _serverConfig = serverConfig;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Conversation Cleanup Service starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Check for expired conversations
                    int expiredConversations = await _conversationStateRepository.CleanupMaxDurationReachedConversationsAsync(_serverConfig.Id, _serverConfig.RegionId, DateTime.UtcNow.AddMinutes(5));

                    // todo log to conversation logs repo
                    //.Push(c => c.Logs, new ConversationStateLogEntry() { Level = ConversationStateLogLevelEnum.Critical, Message = "Expected endtime reached for conversation but it wasnt ended, so manually cleaned up" });

                    // TODO we should log these 3 if they are not 0 for analytics as this should only happen when there seems to be a major error
                    // for now just log
                    if (expiredConversations > 0)
                    {
                        _logger.LogWarning($"Cleanedup: ExpiredConversations: {expiredConversations}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during conversation cleanup");
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }

            _logger.LogInformation("Conversation Cleanup Service stopping");
        }
    }
}
