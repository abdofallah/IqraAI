using IqraCore.Entities.Embedding;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Embedding.Cache
{
    public class EmbeddingCacheRepository
    {
        private readonly ILogger<EmbeddingCacheRepository> _logger;
        private readonly IMongoCollection<EmbeddingCacheEntry> _collection;
        private const string CollectionName = "EmbeddingCache";

        public EmbeddingCacheRepository(ILogger<EmbeddingCacheRepository> logger, IMongoClient client, string databaseName)
        {
            _logger = logger;
            IMongoDatabase database = client.GetDatabase(databaseName);
            _collection = database.GetCollection<EmbeddingCacheEntry>(CollectionName);
            EnsureIndexes();
        }

        private void EnsureIndexes()
        {
            // Index for background cleanup jobs based on overall last access time.
            var lastAccessedIndex = new CreateIndexModel<EmbeddingCacheEntry>(
                Builders<EmbeddingCacheEntry>.IndexKeys.Ascending(e => e.LastAccessedAtUtc),
                new CreateIndexOptions { Name = "LastAccessedIndex" }
            );

            // Index to efficiently find all embeddings referenced by a specific business.
            var businessRefIndex = new CreateIndexModel<EmbeddingCacheEntry>(
                Builders<EmbeddingCacheEntry>.IndexKeys.Ascending("ReferencedBy.BusinessId"),
                new CreateIndexOptions { Name = "BusinessReferenceIndex" }
            );

            _collection.Indexes.CreateMany(new[] { lastAccessedIndex, businessRefIndex });
        }

        public async Task<EmbeddingCacheEntry?> GetAsync(string cacheKey, CancellationToken token = default)
        {
            try
            {
                var filter = Builders<EmbeddingCacheEntry>.Filter.Eq(e => e.Id, cacheKey);
                return await _collection.Find(filter).FirstOrDefaultAsync(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting EmbeddingCacheEntry with key {CacheKey}", cacheKey);
                return null;
            }
        }

        public async Task CreateAsync(EmbeddingCacheEntry entry, CancellationToken token = default)
        {
            try
            {
                await _collection.InsertOneAsync(entry, new InsertOneOptions(), token);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // This is an expected outcome during a race condition. The first process to insert wins.
                _logger.LogWarning("Attempted to create a duplicate EmbeddingCacheEntry with key {CacheKey}. This is acceptable during a race condition.", entry.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating EmbeddingCacheEntry with key {CacheKey}", entry.Id);
                // Re-throwing might be appropriate if the caller needs to know about critical DB failures.
                throw;
            }
        }

        /// <summary>
        /// Updates the top-level LastAccessedAtUtc timestamp. This is a fire-and-forget operation.
        /// </summary>
        public async Task UpdateLastAccessedAsync(string cacheKey, DateTime accessTimeUtc, CancellationToken token = default)
        {
            try
            {
                var filter = Builders<EmbeddingCacheEntry>.Filter.Eq(e => e.Id, cacheKey);
                var update = Builders<EmbeddingCacheEntry>.Update.Set(e => e.LastAccessedAtUtc, accessTimeUtc);
                await _collection.UpdateOneAsync(filter, update, cancellationToken: token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update LastAccessedAt for EmbeddingCacheEntry with key {CacheKey}", cacheKey);
            }
        }

        /// <summary>
        /// Atomically adds or updates a business reference to a cache entry.
        /// </summary>
        public async Task AddOrUpdateBusinessReferenceAsync(string cacheKey, EmbeddingCacheEntryReference newReference, CancellationToken token = default)
        {
            try
            {
                // Step 1: Try to update an existing sub-document using arrayFilters.
                var filter = Builders<EmbeddingCacheEntry>.Filter.Eq(e => e.Id, cacheKey);
                var update = Builders<EmbeddingCacheEntry>.Update
                    .Inc("ReferencedBy.$[elem].ReferencedCount", 1)
                    .AddToSet("ReferencedBy.$[elem].ReferencedByAgents", newReference.ReferencedByAgents.FirstOrDefault())
                    .Set("ReferencedBy.$[elem].LastAccessedAtUtc", newReference.LastAccessedAtUtc);

                var arrayFilters = new[]
                {
                    new BsonDocumentArrayFilterDefinition<EmbeddingCacheEntryReference>(
                        new BsonDocument("elem.BusinessId", newReference.BusinessId)
                        .Add("elem.EmbeddingCacheGroupId", newReference.EmbeddingCacheGroupId)
                        .Add("elem.EmbeddingCacheEmbeddingId", newReference.EmbeddingCacheEmbeddingId))
                };

                var updateResult = await _collection.UpdateOneAsync(filter, update, new UpdateOptions { ArrayFilters = arrayFilters }, token);

                // Step 2: If nothing was modified, the reference doesn't exist. Add it with $push.
                if (updateResult.IsAcknowledged && updateResult.ModifiedCount == 0)
                {
                    var pushUpdate = Builders<EmbeddingCacheEntry>.Update.Push(e => e.ReferencedBy, newReference);
                    await _collection.UpdateOneAsync(filter, pushUpdate, cancellationToken: token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding/updating business reference for EmbeddingCacheEntry {CacheKey}", cacheKey);
            }
        }
    }
}
