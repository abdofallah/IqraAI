using CommunityToolkit.HighPerformance;
using IqraInfrastructure.Repositories.MinIO;
using Microsoft.Extensions.Logging;
using Minio.DataModel.Args;
using System.Drawing;

namespace IqraInfrastructure.Repositories.TTS.Cache
{
    public class TTSAudioCacheStorageRepository
    {
        private readonly ILogger<TTSAudioCacheStorageRepository> _logger;
        private readonly IqraMinioClientFactory _minioClientFactory;
        private readonly string _currentRegion;
        public readonly string _bucketName;

        public TTSAudioCacheStorageRepository(ILogger<TTSAudioCacheStorageRepository> logger, IqraMinioClientFactory minioClientFactory, string currentRegion, string bucketName)
        {
            _logger = logger;
            _minioClientFactory = minioClientFactory;
            _currentRegion = currentRegion;
            _bucketName = bucketName;
        }

        public async Task<ReadOnlyMemory<byte>> GetFileAsByteArrayAsync(string objectPath, CancellationToken token = default, string region = null)
        {
            try
            {
                var stream = new MemoryStream();
                var args = new GetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectPath)
                    .WithCallbackStream(async (s, ct) => await s.CopyToAsync(stream, ct));

                var minioClient = _minioClientFactory.GetLocalClientForRegion(region ?? _currentRegion);
                if (minioClient == null)
                {
                    _logger.LogError("Failed to get Minio client for region {Region}", region ?? _currentRegion);
                    return ReadOnlyMemory<byte>.Empty;
                }

                await minioClient.GetObjectAsync(args, token);
                stream.Position = 0; // Reset position for reading
                return new ReadOnlyMemory<byte>(stream.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get file {ObjectPath} from Minio bucket {BucketName}", objectPath, _bucketName);
                return ReadOnlyMemory<byte>.Empty;
            }
        }

        public async Task PutFileAsByteDataAsync(string objectPath, ReadOnlyMemory<byte> fileBytes, Dictionary<string, string> metaData, CancellationToken token = default, string region = null)
        {
            try
            {
                using var fileStream = fileBytes.AsStream();
                var args = new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectPath)
                    .WithStreamData(fileStream)
                    .WithObjectSize(fileStream.Length)
                    .WithContentType("audio/pcm") // A more specific content type
                    .WithHeaders(metaData);

                var minioClient = _minioClientFactory.GetLocalClientForRegion(region ?? _currentRegion);
                if (minioClient == null)
                {
                    _logger.LogError("Failed to get Minio client for region {Region}", region ?? _currentRegion);
                    return;
                }

                await minioClient.PutObjectAsync(args, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to put file {ObjectPath} to Minio bucket {BucketName}", objectPath, _bucketName);
                // Optionally re-throw if this is a critical failure
            }
        }

        public async Task<bool> FileExistsAsync(string minioPath, CancellationToken none = default, string region = null)
        {
            try
            {
                var minioClient = _minioClientFactory.GetLocalClientForRegion(region ?? _currentRegion);
                if (minioClient == null)
                {
                    _logger.LogError("Failed to get Minio client for region {Region}", region ?? _currentRegion);
                    return false;
                }

                var result = await minioClient.StatObjectAsync(new StatObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(minioPath));

                return result != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get file {ObjectPath} from Minio bucket {BucketName}", minioPath, _bucketName);
                return false;
            }
        }

        public async Task<string?> GetPresignedUrlForGetAsync(string objectPath, string? region = null, int expirySeconds = 3600)
        {
            var minioClient = _minioClientFactory.GetPublicUrlClientForRegion(region ?? _currentRegion);
            if (minioClient == null)
            {
                _logger.LogError("Failed to get Minio client for region {Region}", region ?? _currentRegion);
                return null;
            }

            var args = new PresignedGetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectPath)
                .WithExpiry(expirySeconds);
            return await minioClient.PresignedGetObjectAsync(args);
        }
    }
}
