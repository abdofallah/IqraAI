using IqraCore.Entities.TTS;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.TTS.Cache
{
    /// <summary>
    /// Manages the metadata for TTS audio cache entries in MongoDB.
    /// This repository implements atomic operations to handle concurrency,
    /// race conditions, and reference counting in a distributed environment.
    /// </summary>
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

            // Ensure all necessary indexes are created on application startup.
            EnsureIndexes();
        }

        private void EnsureIndexes()
        {
            // NEW: TTL index to automatically clean up 'GENERATING' documents that were never completed.
            // MongoDB will automatically delete documents where the time in 'ExpiresAtUtc' has passed.
            var ttlIndex = new CreateIndexModel<TTSAudioCacheEntry>(
                Builders<TTSAudioCacheEntry>.IndexKeys.Ascending(e => e.ExpiresAtUtc),
                new CreateIndexOptions { Name = "ExpiresAtUtc_TTL", ExpireAfter = TimeSpan.FromSeconds(0) }
            );

            // This index is useful for analytics or manual cleanup jobs, though the primary lifecycle
            // is now managed by the ReferencedBy array.
            var lastAccessedIndex = new CreateIndexModel<TTSAudioCacheEntry>(
                Builders<TTSAudioCacheEntry>.IndexKeys.Ascending("ReferencedBy.LastAccessedAtUtc"),
                new CreateIndexOptions { Name = "LastAccessedIndex" }
            );

            _collection.Indexes.CreateMany(new[] { ttlIndex, lastAccessedIndex });
        }

        /// <summary>
        /// Retrieves a single cache entry by its key (ID).
        /// </summary>
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

        /// <summary>
        /// Finds multiple cache entries based on a provided filter.
        /// Useful for UI services and background jobs.
        /// </summary>
        public async Task<List<TTSAudioCacheEntry>> FindAsync(FilterDefinition<TTSAudioCacheEntry> filter, CancellationToken token = default)
        {
            try
            {
                return await _collection.Find(filter).ToListAsync(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding TTSAudioCacheEntry documents.");
                return new List<TTSAudioCacheEntry>();
            }
        }

        /// <summary>
        /// The "Claim" operation. Atomically inserts a placeholder document.
        /// Returns true if the claim was successful (we won the race), false otherwise.
        /// </summary>
        public async Task<bool> CreatePlaceholderAsync(TTSAudioCacheEntry placeholder, CancellationToken token = default)
        {
            try
            {
                await _collection.InsertOneAsync(placeholder, new InsertOneOptions(), token);
                return true;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
               return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating placeholder for {CacheKey}", placeholder.Id);
                // In case of unexpected errors, it's safer to assume the claim failed.
                return false;
            }
        }

        /// <summary>
        /// Updates a placeholder to the 'COMPLETE' state after successful generation and upload.
        /// </summary>
        public async Task UpdateToCompleteAsync(string cacheKey, string s3StoragePath, TimeSpan duration, CancellationToken token = default)
        {
            try
            {
                var filter = Builders<TTSAudioCacheEntry>.Filter.Eq(e => e.Id, cacheKey);
                var update = Builders<TTSAudioCacheEntry>.Update
                    .Set(e => e.Status, TTSAudioCacheStatus.COMPLETE)
                    .Set(e => e.S3StorageObjectPath, s3StoragePath)
                    .Set(e => e.Duration, duration)
                    .Set(e => e.LastUpdatedAtUtc, DateTime.UtcNow)
                    .Unset(e => e.ExpiresAtUtc); // IMPORTANT: Remove the TTL expiration to make the entry permanent.

                await _collection.UpdateOneAsync(filter, update, cancellationToken: token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update TTSAudioCacheEntry {CacheKey} to COMPLETE", cacheKey);
            }
        }

        /// <summary>
        /// Updates a placeholder to the 'FAILED' state. The entry will be cleaned up later by the TTL index.
        /// </summary>
        public async Task UpdateToFailedAsync(string cacheKey, string errorMessage, CancellationToken token = default)
        {
            try
            {
                var filter = Builders<TTSAudioCacheEntry>.Filter.Eq(e => e.Id, cacheKey);
                var update = Builders<TTSAudioCacheEntry>.Update
                    .Set(e => e.Status, TTSAudioCacheStatus.FAILED)
                    .Set(e => e.ErrorMessage, errorMessage)
                    .Set(e => e.LastUpdatedAtUtc, DateTime.UtcNow);

                await _collection.UpdateOneAsync(filter, update, cancellationToken: token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update TTSAudioCacheEntry {CacheKey} to FAILED", cacheKey);
            }
        }

        /// <summary>
        /// Atomically adds or updates a business reference to a cache entry.
        /// If a reference for the business/group already exists, its count is incremented.
        /// Otherwise, a new reference sub-document is added to the array.
        /// </summary>
        public async Task AddOrUpdateBusinessReferenceAsync(string cacheKey, TTSAudioCacheEntryReference newReference, CancellationToken token = default)
        {
            try
            {
                // Step 1: Try to update an existing sub-document.
                // We use arrayFilters to identify the specific element in the ReferencedBy array to update.
                var filter = Builders<TTSAudioCacheEntry>.Filter.Eq(e => e.Id, cacheKey);
                var update = Builders<TTSAudioCacheEntry>.Update
                    .Inc("ReferencedBy.$[elem].ReferencedCount", 1)
                    .AddToSet("ReferencedBy.$[elem].ReferencedByAgents", newReference.ReferencedByAgents.FirstOrDefault()) // Assumes one agent at a time
                    .Set("ReferencedBy.$[elem].LastAccessedAtUtc", DateTime.UtcNow);

                var arrayFilters = new[]
                {
                    new BsonDocumentArrayFilterDefinition<TTSAudioCacheEntryReference>(
                        new BsonDocument("elem.BusinessId", newReference.BusinessId)
                        .Add("elem.AudioCacheGroupId", newReference.AudioCacheGroupId)
                        .Add("elem.AudioCacheEntryId", newReference.AudioCacheEntryId))
                };

                var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };

                var result = await _collection.UpdateOneAsync(filter, update, updateOptions, token);

                // Step 2: If no document was modified, it means the sub-document didn't exist. Add it now.
                if (result.IsAcknowledged && result.ModifiedCount == 0)
                {
                    var pushUpdate = Builders<TTSAudioCacheEntry>.Update.Push(e => e.ReferencedBy, newReference);
                    await _collection.UpdateOneAsync(filter, pushUpdate, cancellationToken: token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding/updating business reference for {CacheKey}", cacheKey);
            }
        }

        /// <summary>
        /// Atomically removes a business reference from a cache entry using the $pull operator.
        /// </summary>
        public async Task RemoveBusinessReferenceAsync(string cacheKey, long businessId, string audioCacheGroupId, string audioCacheEntryId, CancellationToken token = default)
        {
            try
            {
                var filter = Builders<TTSAudioCacheEntry>.Filter.Eq(e => e.Id, cacheKey);
                var update = Builders<TTSAudioCacheEntry>.Update.PullFilter(e => e.ReferencedBy, r =>
                    r.BusinessId == businessId &&
                    r.AudioCacheGroupId == audioCacheGroupId &&
                    r.AudioCacheEntryId == audioCacheEntryId);

                await _collection.UpdateOneAsync(filter, update, cancellationToken: token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing business reference for {CacheKey}", cacheKey);
            }
        }

        /// <summary>
        /// Permanently deletes a cache entry document. To be used by the cleanup service.
        /// </summary>
        public async Task DeleteAsync(string cacheKey, CancellationToken token = default)
        {
            try
            {
                var filter = Builders<TTSAudioCacheEntry>.Filter.Eq(e => e.Id, cacheKey);
                await _collection.DeleteOneAsync(filter, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting TTSAudioCacheEntry with key {CacheKey}", cacheKey);
            }
        }
    }
}
