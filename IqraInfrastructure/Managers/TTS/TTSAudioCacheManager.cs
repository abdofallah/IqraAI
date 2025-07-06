using IqraCore.Entities.TTS;
using IqraCore.Interfaces.TTS;
using IqraInfrastructure.Repositories.TTS.Cache;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IqraInfrastructure.Managers.TTS
{
    public record RedisCachePointer(string Path, TimeSpan Duration);
    public class TTSAudioCacheManager
    {
        private readonly ILogger<TTSAudioCacheManager> _logger;
        private readonly TTSAudioCacheIndexRepository _redisRepo;
        private readonly TTSAudioCacheMetadataRepository _mongoRepo;
        private readonly TTSAudioCacheStorageRepository _minioRepo;
        private readonly TimeSpan _redisTTL = TimeSpan.FromHours(24);

        public TTSAudioCacheManager(
            ILogger<TTSAudioCacheManager> logger,
            TTSAudioCacheIndexRepository redisRepo,
            TTSAudioCacheMetadataRepository mongoRepo,
            TTSAudioCacheStorageRepository minioRepo)
        {
            _logger = logger;
            _redisRepo = redisRepo;
            _mongoRepo = mongoRepo;
            _minioRepo = minioRepo;
        }

        public async Task<(bool hit, ReadOnlyMemory<byte> audioData, TimeSpan duration)> GetAudioFromCacheAsync(string cacheKey, CancellationToken token)
        {
            // --- Stage 1: Check fast, region-local Redis cache ---
            var (redisSuccess, redisValue) = await _redisRepo.GetAsync(cacheKey);
            if (redisSuccess && redisValue != null)
            {
                _logger.LogDebug("Cache HIT (Redis) for key: {CacheKey}", cacheKey);
                var pointer = JsonSerializer.Deserialize<RedisCachePointer>(redisValue);
                var audioBytes = await _minioRepo.GetFileAsByteArrayAsync(pointer.Path, token);

                if (!audioBytes.IsEmpty)
                {
                    // Fire-and-forget update of the last accessed time in MongoDB
                    _ = _mongoRepo.UpdateLastAccessedAsync(cacheKey, DateTime.UtcNow, CancellationToken.None);
                    return (true, audioBytes, pointer.Duration);
                }
            }

            // --- Stage 2: If Redis missed, check persistent Global MongoDB ---
            _logger.LogDebug("Cache MISS (Redis), checking MongoDB for key: {CacheKey}", cacheKey);
            var mongoEntry = await _mongoRepo.GetAsync(cacheKey, token);
            if (mongoEntry != null)
            {
                _logger.LogInformation("Cache HIT (MongoDB) for key: {CacheKey}", cacheKey);
                var audioBytes = await _minioRepo.GetFileAsByteArrayAsync(mongoEntry.MinioObjectPath, token);
                if (!audioBytes.IsEmpty)
                {
                    // Re-populate the local Redis cache for the next request in this region
                    var pointer = new RedisCachePointer(mongoEntry.MinioObjectPath, mongoEntry.Duration);
                    var newRedisValue = JsonSerializer.Serialize(pointer);
                    _ = _redisRepo.SetAsync(cacheKey, newRedisValue, _redisTTL);

                    // Update the last accessed time
                    _ = _mongoRepo.UpdateLastAccessedAsync(cacheKey, DateTime.UtcNow, CancellationToken.None);
                    return (true, audioBytes, mongoEntry.Duration);
                }
            }

            _logger.LogInformation("Cache MISS (Complete) for key: {CacheKey}", cacheKey);
            return (false, ReadOnlyMemory<byte>.Empty, TimeSpan.Zero);
        }

        public async Task StoreAudioInCacheAsync(string cacheKey, ReadOnlyMemory<byte> audioData, TimeSpan duration, ITtsConfig config, TTSAudioCacheEntry context)
        {
            _logger.LogInformation("Storing new audio in cache for key: {CacheKey}", cacheKey);
            var now = DateTime.UtcNow;
            var minioPath = $"cache/tts-{((int)context.ProviderName)}/{cacheKey}.pcm";

            // --- Stage 1: Store the audio blob in Minio ---
            await _minioRepo.PutFileAsByteDataAsync(minioPath, audioData, new Dictionary<string, string> { { "cacheKey", cacheKey } });

            // --- Stage 2: Create and store the full metadata entry in MongoDB ---
            var entry = new TTSAudioCacheEntry
            {
                Id = cacheKey,
                ProviderName = context.ProviderName,
                TtsConfigJson = JsonSerializer.Serialize(config, config.GetType()),
                TtsConfigVersion = config.ConfigVersion,
                MinioObjectPath = minioPath,
                Duration = duration,
                CreatedAtUtc = now,
                LastAccessedAtUtc = now,
                BusinessId = context.BusinessId,
                AgentId = context.AgentId
            };
            await _mongoRepo.CreateAsync(entry);

            // --- Stage 3: Set the fast-access pointer in the local Redis cache ---
            var pointer = new RedisCachePointer(minioPath, duration);
            var redisValue = JsonSerializer.Serialize(pointer);
            await _redisRepo.SetAsync(cacheKey, redisValue, _redisTTL);
        }
    }
}
