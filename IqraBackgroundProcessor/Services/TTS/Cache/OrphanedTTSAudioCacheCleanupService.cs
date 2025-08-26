using IqraCore.Entities.TTS;
using IqraInfrastructure.Repositories.TTS.Cache;
using MongoDB.Driver;

namespace IqraBackgroundProcessor.Services.TTS.Cache
{
    public class OrphanedTTSAudioCacheCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrphanedTTSAudioCacheCleanupService> _logger;

        public OrphanedTTSAudioCacheCleanupService(IServiceProvider serviceProvider, ILogger<OrphanedTTSAudioCacheCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Orphaned Cache Cleanup Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var metadataRepo = scope.ServiceProvider.GetRequiredService<TTSAudioCacheMetadataRepository>();
                    var storageRepo = scope.ServiceProvider.GetRequiredService<TTSAudioCacheStorageRepository>();

                    // Find entries with no references
                    var orphanedFilter = Builders<TTSAudioCacheEntry>.Filter.And(
                        Builders<TTSAudioCacheEntry>.Filter.Eq(e => e.Status, TTSAudioCacheStatus.COMPLETE),
                        Builders<TTSAudioCacheEntry>.Filter.Size(e => e.ReferencedBy, 0)
                    );

                    var orphans = await metadataRepo.FindAsync(orphanedFilter, stoppingToken);

                    foreach (var orphan in orphans)
                    {
                        // 1. Delete file from MinIO (from its origin region)
                        // await storageRepo.DeleteFileAsync(orphan.MinioObjectPath, orphan.OriginRegion);

                        // 2. Delete entry from MongoDB
                        // await metadataRepo.DeleteAsync(orphan.Id);
                    }
                }

                // Wait for the next cycle, e.g., 6 hours
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
        }
    }
}
