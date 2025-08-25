using IqraCore.Entities.TTS;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.TTS.Cache
{
    public class TTSAudioCacheMetadataRepository
    {
        private readonly ILogger<TTSAudioCacheMetadataRepository> _logger;
        private readonly IMongoCollection<TTSAudioCacheEntry> _collection;
        private const string CollectionName = "TTSAudioCache";

        public TTSAudioCacheMetadataRepository(ILogger<TTSAudioCacheMetadataRepository> logger, IMongoClient client, string databaseName)
        {
            _logger = logger;
            IMongoDatabase database = client.GetDatabase(databaseName);
            _collection = database.GetCollection<TTSAudioCacheEntry>(CollectionName);

            // It's good practice to ensure indexes exist. This can be run on startup.
            EnsureIndexes();
        }

        private void EnsureIndexes()
        {
            // This index is useful for a future cleanup job based on TTL.
            var lastAccessedIndex = new CreateIndexModel<TTSAudioCacheEntry>(
                Builders<TTSAudioCacheEntry>.IndexKeys.Ascending(e => e.LastAccessedAtUtc),
                new CreateIndexOptions { Name = "LastAccessedIndex" }
            );
            _collection.Indexes.CreateOne(lastAccessedIndex);
        }

        public async Task<TTSAudioCacheEntry?> GetAsync(string cacheKey, CancellationToken token = default)
        {
            try
            {
                var filter = Builders<TTSAudioCacheEntry>.Filter.Eq(e => e.Id, cacheKey);
                return await _collection.Find(filter).FirstOrDefaultAsync(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting TTSAudioCacheEntry with key {CacheKey}", cacheKey);
                return null;
            }
        }

        public async Task CreateAsync(TTSAudioCacheEntry entry, CancellationToken token = default)
        {
            try
            {
                // We use InsertOneAsync. If another server created the same entry in the last millisecond,
                // this will throw a MongoWriteException due to the duplicate _id key. This is the
                // desired behavior to prevent duplicate entries, so we just log it and continue.
                await _collection.InsertOneAsync(entry, new InsertOneOptions(), token);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                _logger.LogWarning("Attempted to create a duplicate TTSAudioCacheEntry with key {CacheKey}. This is acceptable during a race condition.", entry.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating TTSAudioCacheEntry with key {CacheKey}", entry.Id);
            }
        }

        public async Task UpdateLastAccessedAsync(string cacheKey, DateTime accessTimeUtc, CancellationToken token = default)
        {
            try
            {
                var filter = Builders<TTSAudioCacheEntry>.Filter.Eq(e => e.Id, cacheKey);
                var update = Builders<TTSAudioCacheEntry>.Update.Set(e => e.LastAccessedAtUtc, accessTimeUtc);

                // We don't need to wait for this result. It's a "fire-and-forget" update.
                await _collection.UpdateOneAsync(filter, update, cancellationToken: token);
            }
            catch (Exception ex)
            {
                // Log the error but don't let it fail the main operation.
                _logger.LogWarning(ex, "Failed to update LastAccessedAt for TTSAudioCacheEntry with key {CacheKey}", cacheKey);
            }
        }

        public async Task<bool> ExistsAsync(string cacheKey, CancellationToken none)
        {
            try
            {
                var filter = Builders<TTSAudioCacheEntry>.Filter.Eq(e => e.Id, cacheKey);
                return await _collection.Find(filter).AnyAsync(none);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error checking existence of TTSAudioCacheEntry with key {CacheKey}", cacheKey);
                return false;
            }
        }
    }
}
