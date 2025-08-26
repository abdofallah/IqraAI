using IqraCore.Entities.Embedding;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.Embedding;
using IqraCore.Models.Embedding.Cache;
using IqraInfrastructure.Repositories.Embedding.Cache;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Embedding
{
    public record BusinessReferenceInfo(long BusinessId, string GroupId, string EmbeddingId, string AgentId);

    public class EmbeddingCacheManager
    {
        private readonly ILogger<EmbeddingCacheManager> _logger;
        private readonly EmbeddingCacheRepository _repository;

        public EmbeddingCacheManager(ILogger<EmbeddingCacheManager> logger, EmbeddingCacheRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        #region Read Path

        public async Task<EmbeddingCacheGetResult> TryGetEmbeddingAsync(string cacheKey, BusinessReferenceInfo businessReference, CancellationToken token)
        {
            var cachedEntry = await _repository.GetAsync(cacheKey, token);

            if (cachedEntry != null)
            {
                _logger.LogDebug("Cache HIT for embedding key {CacheKey}", cacheKey);

                // Fire-and-forget the updates so the current request is not blocked.
                _ = UpdateReferencesOnHitAsync(cacheKey, businessReference, token);

                return new EmbeddingCacheGetResult(CacheHitStatus.HIT, cachedEntry.Vector);
            }

            _logger.LogDebug("Cache MISS for embedding key {CacheKey}", cacheKey);
            return EmbeddingCacheGetResult.Miss();
        }

        private async Task UpdateReferencesOnHitAsync(string cacheKey, BusinessReferenceInfo businessReference, CancellationToken token)
        {
            var now = DateTime.UtcNow;
            var reference = CreateReferenceFromInfo(businessReference, now);

            // Atomically update the specific business reference.
            await _repository.AddOrUpdateBusinessReferenceAsync(cacheKey, reference, token);

            // Update the top-level last accessed time.
            await _repository.UpdateLastAccessedAsync(cacheKey, now, token);
        }

        #endregion

        #region Write Path

        public async Task StoreEmbeddingAsync(
            string cacheKey,
            List<float> vector,
            string originalText,
            InterfaceEmbeddingProviderEnum providerType,
            IEmbeddingConfig config,
            BusinessReferenceInfo initialBusinessReference)
        {
            var now = DateTime.UtcNow;

            var newEntry = new EmbeddingCacheEntry
            {
                Id = cacheKey,
                ProviderName = providerType,
                EmbeddingConfigJson = JsonSerializer.Serialize(config, config.GetType()),
                EmbeddingConfigVersion = config.ConfigVersion,
                OriginalText = originalText,
                Vector = vector,
                CreatedAtUtc = now,
                LastAccessedAtUtc = now,
                ReferencedBy = new List<EmbeddingCacheEntryReference>
                {
                    CreateReferenceFromInfo(initialBusinessReference, now)
                }
            };

            // The repository's CreateAsync gracefully handles the race condition.
            // If another process inserts an entry with the same key first, this will be a no-op.
            try
            {
                await _repository.CreateAsync(newEntry);
                _logger.LogInformation("Successfully stored new embedding for cache key {CacheKey}", cacheKey);
            }
            catch (Exception ex)
            {
                // The repository already logs the specific error.
                // We catch here to prevent the fire-and-forget task from becoming an unhandled exception.
                _logger.LogError(ex, "Failed to store embedding for cache key {CacheKey}", cacheKey);
            }
        }

        #endregion

        private EmbeddingCacheEntryReference CreateReferenceFromInfo(BusinessReferenceInfo info, DateTime now)
        {
            return new EmbeddingCacheEntryReference
            {
                BusinessId = info.BusinessId,
                EmbeddingCacheGroupId = info.GroupId,
                EmbeddingCacheEmbeddingId = info.EmbeddingId,
                ReferencedByAgents = new List<string> { info.AgentId },
                ReferencedCount = 1,
                LastAccessedAtUtc = now
            };
        }
    }
}
