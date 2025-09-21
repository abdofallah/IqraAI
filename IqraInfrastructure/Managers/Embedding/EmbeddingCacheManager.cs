using IqraCore.Entities.Business;
using IqraCore.Entities.Embedding;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.Embedding;
using IqraCore.Models.Embedding.Cache;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.Embedding.Cache;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Embedding
{
    public record BusinessReferenceInfo(long BusinessId, string EmbeddingGroupId, string EmbeddingGroupLanguage, string EmbeddingGroupEntryId, string AgentId);

    public class EmbeddingCacheManager
    {
        private readonly ILogger<EmbeddingCacheManager> _logger;
        private readonly EmbeddingCacheRepository _repository;
        private readonly BusinessAppRepository _businessAppRepository;

        public EmbeddingCacheManager(ILogger<EmbeddingCacheManager> logger, EmbeddingCacheRepository repository, BusinessAppRepository businessAppRepository)
        {
            _logger = logger;
            _repository = repository;
            _businessAppRepository = businessAppRepository;
        }

        #region Read Path

        public async Task<EmbeddingCacheGetResult> TryGetEmbeddingAsync(string cacheKey, InterfaceEmbeddingProviderEnum providerType, IEmbeddingConfig config, BusinessReferenceInfo businessReference, CancellationToken token)
        {
            var cachedEntry = await _repository.GetAsync(cacheKey, token);
            if (cachedEntry != null)
            {
                _ = UpdateReferencesOnHitAsync(cacheKey, businessReference, token);
                _ = CheckAndUpdateEmbeddingLink(cacheKey, config.ConfigVersion, providerType, businessReference, token);
                return new EmbeddingCacheGetResult(CacheHitStatus.HIT, cachedEntry.Vector);
            }

            return EmbeddingCacheGetResult.Miss();
        }

        #endregion

        #region Write Path

        public async Task StoreEmbeddingAsync(
            string cacheKey,
            List<float> vector,
            string originalText,
            InterfaceEmbeddingProviderEnum providerType,
            IEmbeddingConfig config,
            BusinessReferenceInfo initialBusinessReference,
            CancellationToken token = default
        )
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
                ReferencedBy = new List<EmbeddingCacheEntryReference>()
            };

            // The repository's CreateAsync gracefully handles the race condition.
            // If another process inserts an entry with the same key first, this will be a no-op.
            try
            {
                await _repository.CreateAsync(newEntry);
                await UpdateReferencesOnHitAsync(cacheKey, initialBusinessReference, token);
                await CheckAndUpdateEmbeddingLink(cacheKey, config.ConfigVersion, providerType, initialBusinessReference, token);
            }
            catch (Exception ex)
            {
                // The repository already logs the specific error.
                // We catch here to prevent the fire-and-forget task from becoming an unhandled exception.
                _logger.LogError(ex, "Failed to store embedding for cache key {CacheKey}", cacheKey);
            }
        }

        #endregion

        private async Task CheckAndUpdateEmbeddingLink(string cacheKey, int configVersion, InterfaceEmbeddingProviderEnum provider, BusinessReferenceInfo businessReference, CancellationToken token)
        {
            var cacheLink = new BusinessAppCacheEmbeddingCacheLink()
            {
                CacheKey = cacheKey,
                ConfigVersion = configVersion,
                Provider = provider
            };

            await _businessAppRepository.AddCacheLinkToEmbeddingCacheGroupEntry(
                businessReference.BusinessId,
                businessReference.EmbeddingGroupId,
                businessReference.EmbeddingGroupEntryId,
                businessReference.EmbeddingGroupLanguage,
                cacheLink
            );
        }

        private async Task UpdateReferencesOnHitAsync(string cacheKey, BusinessReferenceInfo businessReference, CancellationToken token)
        {
            var now = DateTime.UtcNow;
            var reference = CreateReferenceFromInfo(businessReference, now);

            await _repository.AddOrUpdateBusinessReferenceAsync(cacheKey, reference, token);
            await _repository.UpdateLastAccessedAsync(cacheKey, now, token);
        }

        private EmbeddingCacheEntryReference CreateReferenceFromInfo(BusinessReferenceInfo info, DateTime now)
        {
            return new EmbeddingCacheEntryReference
            {
                BusinessId = info.BusinessId,
                EmbeddingCacheGroupId = info.EmbeddingGroupId,
                EmbeddingCacheGroupEmbeddingLanguage = info.EmbeddingGroupLanguage,
                EmbeddingCacheEmbeddingId = info.EmbeddingGroupEntryId,
                ReferencedByAgents = new List<string> { info.AgentId },
                ReferencedCount = 1,
                LastAccessedAtUtc = now
            };
        }
    }
}
