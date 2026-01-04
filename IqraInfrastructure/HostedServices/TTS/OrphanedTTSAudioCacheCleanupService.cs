using IqraCore.Entities.TTS;
using IqraInfrastructure.Repositories.TTS.Cache;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IqraInfrastructure.HostedServices.TTS
{
    public class OrphanedTTSAudioCacheCleanupService : BackgroundService
    {
        private readonly ILogger<OrphanedTTSAudioCacheCleanupService> _logger;
        private readonly TTSAudioCacheMetadataRepository _metadataRepo;
        private readonly TTSAudioCacheStorageRepository _storageRepo;

        public OrphanedTTSAudioCacheCleanupService(
            ILogger<OrphanedTTSAudioCacheCleanupService> logger,
            TTSAudioCacheMetadataRepository metadataRepo,
            TTSAudioCacheStorageRepository storageRepo
        )
        {
            _logger = logger;
            _metadataRepo = metadataRepo;
            _storageRepo = storageRepo;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Orphaned Cache Cleanup Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                // Find entries with no references
                var orphanedFilter = Builders<TTSAudioCacheEntry>.Filter.And(
                    Builders<TTSAudioCacheEntry>.Filter.Eq(e => e.Status, TTSAudioCacheStatus.COMPLETE),
                    Builders<TTSAudioCacheEntry>.Filter.Size(e => e.ReferencedBy, 0)
                );

                var orphans = await _metadataRepo.FindAsync(orphanedFilter, stoppingToken);

                foreach (var orphan in orphans)
                {
                    // 1. Delete file from S3Storage (from its origin region)
                    // await storageRepo.DeleteFileAsync(orphan.S3StorageObjectPath, orphan.OriginRegion);

                    // 2. Delete entry from MongoDB
                    // await metadataRepo.DeleteAsync(orphan.Id);
                }

                // Wait for the next cycle, e.g., 6 hours
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
        }
    }
}