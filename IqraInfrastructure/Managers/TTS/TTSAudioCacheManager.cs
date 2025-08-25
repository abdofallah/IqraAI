using IqraCore.Entities.Interfaces;
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

        private readonly TTSAudioCacheIndexRepository _cacheIndexLocalRepoistory;
        private readonly TTSAudioCacheMetadataRepository _cacheMetadataRepoistory;
        private readonly TTSAudioCacheStorageRepository _cacheAudioStorage;

        private readonly TimeSpan _redisTTL = TimeSpan.FromHours(24);

        public TTSAudioCacheManager(
            ILogger<TTSAudioCacheManager> logger,
            TTSAudioCacheIndexRepository redisRepo,
            TTSAudioCacheMetadataRepository mongoRepo,
            TTSAudioCacheStorageRepository minioRepo)
        {
            _logger = logger;
            _cacheIndexLocalRepoistory = redisRepo;
            _cacheMetadataRepoistory = mongoRepo;
            _cacheAudioStorage = minioRepo;
        }

        public async Task<(bool hit, ReadOnlyMemory<byte> audioData, TimeSpan duration)> GetAudioFromCacheAsync(string cacheKey, long businessId, CancellationToken token)
        {
            bool hitResult = false;

            try
            {
                // Check fast, region-local Redis cache
                var (redisSuccess, redisValue) = await _cacheIndexLocalRepoistory.GetAsync(cacheKey);
                if (redisSuccess && redisValue != null)
                {
                    var pointer = JsonSerializer.Deserialize<RedisCachePointer>(redisValue);
                    var audioBytes = await _cacheAudioStorage.GetFileAsByteArrayAsync(pointer.Path, token);

                    if (!audioBytes.IsEmpty)
                    {
                        hitResult = true;

                        return (hitResult, audioBytes, pointer.Duration);
                    }
                    else
                    {
                        // this logic is similar to that of mongo audio bytes empty

                        // TODO this means the file was not there in the minio repo
                        // could have been generated in other region and not available in current
                        // or the file just simply does not exist, for now just ignore
                        return (hitResult, ReadOnlyMemory<byte>.Empty, TimeSpan.Zero);
                    }
                }

                // If Redis missed, check persistent Global MongoDB
                var mongoEntry = await _cacheMetadataRepoistory.GetAsync(cacheKey, token);
                if (mongoEntry != null)
                {
                    var audioBytes = await _cacheAudioStorage.GetFileAsByteArrayAsync(mongoEntry.MinioObjectPath, token);
                    if (!audioBytes.IsEmpty)
                    {
                        hitResult = true;

                        var pointer = new RedisCachePointer(mongoEntry.MinioObjectPath, mongoEntry.Duration);
                        var newRedisValue = JsonSerializer.Serialize(pointer);
                        _ = _cacheIndexLocalRepoistory.SetAsync(cacheKey, newRedisValue, _redisTTL);

                        return (hitResult, audioBytes, mongoEntry.Duration);
                    }
                    else
                    {
                        // this logic is similar to that of redis audio bytes empty

                        // TODO this means the file was not there in the minio repo
                        // could have been generated in other region and not available in current
                        // or the file just simply does not exist, for now just ignore
                        return (hitResult, ReadOnlyMemory<byte>.Empty, TimeSpan.Zero);
                    }
                }

                return (hitResult, ReadOnlyMemory<byte>.Empty, TimeSpan.Zero);
            }
            finally
            {
                if (hitResult)
                {
                    _ = _cacheMetadataRepoistory.UpdateLastAccessedAsync(cacheKey, DateTime.UtcNow, CancellationToken.None);
                    // TODO: we need to reference this cache part of the business caching system as well
                    // multiple businesses can reference to the same cache, if a business deletes it, we need to make sure if other is using it then keep else if no reference, then delete
                }
            }
        }

        public async Task StoreAudioInCacheAsync(string cacheKey, ReadOnlyMemory<byte> audioData, TimeSpan duration, ITTSConfig config, InterfaceTTSProviderEnum ttsProvider)
        {
            var minioPath = $"cache/tts-{((int)ttsProvider)}/{cacheKey}.pcm";

            // Store the audio blob in Minio
            var checkFileWithCacheExists = await _cacheAudioStorage.FileExistsAsync(minioPath, CancellationToken.None);
            if (!checkFileWithCacheExists)
            {
                await _cacheAudioStorage.PutFileAsByteDataAsync(minioPath, audioData, new Dictionary<string, string> { { "cacheKey", cacheKey } });
            }

            // Create and store the full metadata entry in MongoDB
            var checkEntryExists = await _cacheMetadataRepoistory.ExistsAsync(cacheKey, CancellationToken.None);
            if (!checkEntryExists)
            {
                var now = DateTime.UtcNow;

                var entry = new TTSAudioCacheEntry
                {
                    Id = cacheKey,
                    ProviderName = ttsProvider,
                    TtsConfigJson = JsonSerializer.Serialize(config, config.GetType()),
                    TtsConfigVersion = config.ConfigVersion,
                    MinioObjectPath = minioPath,
                    Duration = duration,
                    CreatedAtUtc = now,
                    LastAccessedAtUtc = now

                };
                await _cacheMetadataRepoistory.CreateAsync(entry);
            }

            // Set the fast-access pointer in the local Redis cache
            var pointer = new RedisCachePointer(minioPath, duration);
            var redisValue = JsonSerializer.Serialize(pointer);
            await _cacheIndexLocalRepoistory.SetAsync(cacheKey, redisValue, _redisTTL);
        }
    }
}
