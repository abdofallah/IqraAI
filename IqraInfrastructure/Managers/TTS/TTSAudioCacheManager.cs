using IqraCore.Entities.Business;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraCore.Interfaces.TTS;
using IqraCore.Models.TTS.Cache;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.TTS.Cache;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.Text.Json;

namespace IqraInfrastructure.Managers.TTS
{
    public class TTSAudioCacheManager
    {
        private readonly ILogger<TTSAudioCacheManager> _logger;
        private readonly TTSAudioCacheIndexRepository _cacheIndexLocalRepository;
        private readonly TTSAudioCacheMetadataRepository _cacheMetadataRepository;
        private readonly TTSAudioCacheStorageRepository _cacheAudioStorage;
        private readonly BusinessAppRepository _businessAppRepository;
        private readonly string _currentRegion;

        private readonly TimeSpan _redisTTL = TimeSpan.FromHours(24);
        private static readonly TimeSpan GenerationClaimTTL = TimeSpan.FromMinutes(5);

        private readonly AsyncRetryPolicy<TTSAudioCacheEntry> _waitForGenerationPolicy;

        public TTSAudioCacheManager(
            ILogger<TTSAudioCacheManager> logger,

            TTSAudioCacheIndexRepository redisRepo,
            TTSAudioCacheMetadataRepository mongoRepo,
            TTSAudioCacheStorageRepository minioRepo,
            BusinessAppRepository businessAppRepository,

            string currentRegion
        )
        {
            _logger = logger;

            _cacheIndexLocalRepository = redisRepo;
            _cacheMetadataRepository = mongoRepo;
            _cacheAudioStorage = minioRepo;
            _businessAppRepository = businessAppRepository;

            _currentRegion = currentRegion;

            _waitForGenerationPolicy = Policy
               .HandleResult<TTSAudioCacheEntry>(entry => entry == null || entry.Status == TTSAudioCacheStatus.GENERATING)
               .WaitAndRetryAsync(15,
                   retryAttempt => TimeSpan.FromMilliseconds(100),
                   onRetry: (outcome, timespan, retryAttempt, context) =>
                   {
                       string cacheKey = context["cacheKey"].ToString();
                   });
        }

        public async Task<CacheGetResult> TryGetAudioAsync(string cacheKey, ITTSConfig config, InterfaceTTSProviderEnum ttsProvider, long businessId, string audioCacheGroupId, string audioCacheGroupEntryLanguage, string audioCacheGroupEntryId, CancellationToken token)
        {
            // 1. Check fast, region-local Redis cache
            var (redisSuccess, redisValue) = await _cacheIndexLocalRepository.GetAsync(cacheKey);
            if (redisSuccess && redisValue != null)
            {
                var pointer = JsonSerializer.Deserialize<RedisCachePointer>(redisValue);
                var audioBytes = await SmartFetchFromStorageAsync(pointer.Path, pointer.OriginRegion, token);
                if (!audioBytes.IsEmpty)
                {
                    _ = CheckAndUpdateBusinessAudioCacheLink(cacheKey, config.ConfigVersion, ttsProvider, businessId, audioCacheGroupId, audioCacheGroupEntryLanguage, audioCacheGroupEntryId, CancellationToken.None);
                    return new CacheGetResult(CacheHitStatus.HIT, audioBytes, pointer.Duration);
                }
                _logger.LogWarning("Redis entry existed for {CacheKey}, but failed to fetch from storage.", cacheKey);
            }

            // 2. Check persistent Global MongoDB
            var mongoEntry = await _cacheMetadataRepository.GetAsync(cacheKey, token);
            if (mongoEntry == null)
            {
                return CacheGetResult.Miss();
            }

            switch (mongoEntry.Status)
            {
                case TTSAudioCacheStatus.COMPLETE:
                    {
                        _ = CheckAndUpdateBusinessAudioCacheLink(cacheKey, config.ConfigVersion, ttsProvider, businessId, audioCacheGroupId, audioCacheGroupEntryLanguage, audioCacheGroupEntryId, CancellationToken.None);
                        return await SmartFetchAndReturnResultAsync(mongoEntry, token);
                    }

                case TTSAudioCacheStatus.GENERATING:
                    var entryAfterWait = await WaitForGenerationAsync(cacheKey, token);

                    if (entryAfterWait?.Status == TTSAudioCacheStatus.COMPLETE)
                    {
                        _ = CheckAndUpdateBusinessAudioCacheLink(cacheKey, config.ConfigVersion, ttsProvider, businessId, audioCacheGroupId, audioCacheGroupEntryLanguage, audioCacheGroupEntryId, CancellationToken.None);
                        return await SmartFetchAndReturnResultAsync(entryAfterWait, token);
                    }
                    else
                    {
                        _logger.LogWarning("Bailed on waiting for {CacheKey}. Treating as cache miss to prioritize user latency.", cacheKey);
                        return CacheGetResult.Miss(); // Bailout!
                    }

                case TTSAudioCacheStatus.FAILED:
                default:
                    _logger.LogWarning("Cache MISS for {CacheKey}. Found an entry with status: {Status}", cacheKey, mongoEntry.Status);
                    return CacheGetResult.Miss();
            }
        }

        private async Task<TTSAudioCacheEntry> WaitForGenerationAsync(string cacheKey, CancellationToken token)
        {
            var context = new Context($"WaitForGeneration-{cacheKey}", new Dictionary<string, object> { { "cacheKey", cacheKey } });
            // The policy's internal timer will trigger, but we also link to the request's cancellation token.
            try
            {
                return await _waitForGenerationPolicy.ExecuteAsync(ctx => _cacheMetadataRepository.GetAsync(cacheKey, ctx), token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during 'Wait-and-Bail' for {CacheKey}", cacheKey);
                return null;
            }
        }

        private async Task<CacheGetResult> SmartFetchAndReturnResultAsync(TTSAudioCacheEntry entry, CancellationToken token)
        {
            var audioBytes = await SmartFetchFromStorageAsync(entry.MinioObjectPath, entry.OriginRegion, token);
            if (!audioBytes.IsEmpty)
            {
                // Refresh the Redis cache as we go. Fire-and-forget.
                _ = UpdateRedisCacheAsync(entry);
                return new CacheGetResult(CacheHitStatus.HIT, audioBytes, entry.Duration!.Value);
            }

            _logger.LogError("CRITICAL: Failed to fetch cache object {Path} for {CacheKey} from any region.",
                entry.MinioObjectPath, entry.Id);
            return CacheGetResult.Miss();
        }

        private async Task<ReadOnlyMemory<byte>> SmartFetchFromStorageAsync(string objectPath, string originRegion, CancellationToken token)
        {
            // Attempt 1: Fetch from the current, local region
            var audioBytes = await _cacheAudioStorage.GetFileAsByteArrayAsync(objectPath, token, _currentRegion);
            if (!audioBytes.IsEmpty) return audioBytes;

            // Attempt 2: If local failed and we are not in the origin region, try a cross-region fetch
            if (_currentRegion != originRegion)
            {
                // we should log how many times cross region fetch is done and what is the latency
                audioBytes = await _cacheAudioStorage.GetFileAsByteArrayAsync(objectPath, token, originRegion);
                if (!audioBytes.IsEmpty) return audioBytes;
            }

            return ReadOnlyMemory<byte>.Empty;
        }

        public async Task StoreAudioAsync(string cacheKey, ReadOnlyMemory<byte> audioData, TimeSpan duration, ITTSConfig config, InterfaceTTSProviderEnum ttsProvider, long businessId, string audioCacheGroupId, string audioCacheGroupEntryLanguage, string audioCacheGroupEntryId, CancellationToken token)
        {
            var placeholder = new TTSAudioCacheEntry
            {
                Id = cacheKey,
                ProviderName = ttsProvider,
                TtsConfigJson = JsonSerializer.Serialize(config, config.GetType()),
                TtsConfigVersion = config.ConfigVersion,
                Status = TTSAudioCacheStatus.GENERATING,
                OriginRegion = _currentRegion,
                CreatedAtUtc = DateTime.UtcNow,
                LastUpdatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.Add(GenerationClaimTTL)
            };

            bool weWonTheRace = await _cacheMetadataRepository.CreatePlaceholderAsync(placeholder);
            if (weWonTheRace)
            {
                try
                {
                    var minioPath = $"cache/tts-{(int)ttsProvider}/{cacheKey}.pcm";

                    await _cacheAudioStorage.PutFileAsByteDataAsync(minioPath, audioData, new Dictionary<string, string>(), region: _currentRegion);
                    await _cacheMetadataRepository.UpdateToCompleteAsync(cacheKey, minioPath, duration);
                    var finalEntry = await _cacheMetadataRepository.GetAsync(cacheKey);
                    await UpdateRedisCacheAsync(finalEntry);
                    await CheckAndUpdateBusinessAudioCacheLink(cacheKey, config.ConfigVersion, ttsProvider, businessId, audioCacheGroupId, audioCacheGroupEntryLanguage, audioCacheGroupEntryId, token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Storage failed for {CacheKey} after winning the claim race.", cacheKey);
                    await _cacheMetadataRepository.UpdateToFailedAsync(cacheKey, ex.Message);
                }
            }
            else
            {
                // We lost the race. Another process is already generating and storing this.
                // Our job is done. We simply log it and do nothing further.
            }
        }

        private async Task CheckAndUpdateBusinessAudioCacheLink(
            string cacheKey,
            int configVersion,
            InterfaceTTSProviderEnum provider,
            long businessId,
            string groupId,
            string groupEntryLanguage,
            string groupEntryId,
            CancellationToken token
        )
        {
            var cacheLink = new BusinessAppCacheAudioCacheLink()
            {
                CacheKey = cacheKey,
                ConfigVersion = configVersion,
                Provider = provider
            };

            await _businessAppRepository.AddCacheLinkToAudioCacheGroupEntry(
                businessId,
                groupId,
                groupEntryId,
                groupEntryLanguage,
                cacheLink
            );
        }

        private async Task UpdateRedisCacheAsync(TTSAudioCacheEntry entry)
        {
            if (entry == null || entry.Status != TTSAudioCacheStatus.COMPLETE) return;

            var pointer = new RedisCachePointer(entry.MinioObjectPath, entry.Duration.Value, entry.OriginRegion);
            var redisValue = JsonSerializer.Serialize(pointer);
            await _cacheIndexLocalRepository.SetAsync(entry.Id, redisValue, _redisTTL);
        }
    }
}
