using Amazon.S3;
using Amazon.S3.Model;
using CommunityToolkit.HighPerformance;
using IqraCore.Entities.App.Configuration;
using IqraInfrastructure.Repositories.S3Storage;
using Microsoft.Extensions.Logging;
using System.Net;

namespace IqraInfrastructure.Repositories.TTS.Cache
{
    public class TTSAudioCacheStorageRepository
    {
        private readonly ILogger<TTSAudioCacheStorageRepository> _logger;
        private readonly S3StorageClientFactory _s3StorageClientFactory;
        private readonly string _bucketName;

        public TTSAudioCacheStorageRepository(ILogger<TTSAudioCacheStorageRepository> logger, S3StorageClientFactory clientFactory)
        {
            _logger = logger;
            _s3StorageClientFactory = clientFactory;
            _bucketName = S3StorageConfig.BusinessTTSAudioCacheStorageRepositoryBucketName;

            var client = S3StorageHelpers.GetS3Client(_s3StorageClientFactory, null);
            S3StorageHelpers.EnsureBucketExistsAsync(client, _bucketName, _logger).GetAwaiter().GetResult();
        }

        public async Task<ReadOnlyMemory<byte>> GetFileAsByteArrayAsync(string objectPath, CancellationToken token = default, string? region = null)
        {
            try
            {
                var client = S3StorageHelpers.GetS3Client(_s3StorageClientFactory, region);

                var request = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = objectPath
                };

                using var response = await client.GetObjectAsync(request, token);
                using var memoryStream = new MemoryStream();

                await response.ResponseStream.CopyToAsync(memoryStream, token);
                return new ReadOnlyMemory<byte>(memoryStream.ToArray());
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
            {
                // File not found is a valid state for a cache; return empty.
                return ReadOnlyMemory<byte>.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get file {ObjectPath} from S3 bucket {BucketName}", objectPath, _bucketName);
                return ReadOnlyMemory<byte>.Empty;
            }
        }

        public async Task PutFileAsByteDataAsync(string objectPath, ReadOnlyMemory<byte> fileBytes, Dictionary<string, string> metaData, CancellationToken token = default, string? region = null)
        {
            try
            {
                var client = S3StorageHelpers.GetS3Client(_s3StorageClientFactory, region);

                using var fileStream = fileBytes.AsStream();

                var request = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = objectPath,
                    InputStream = fileStream,
                    ContentType = "audio/pcm" // Specific content type per requirements
                };

                if (metaData != null)
                {
                    foreach (var kvp in metaData)
                    {
                        request.Metadata.Add(kvp.Key, kvp.Value);
                    }
                }

                await client.PutObjectAsync(request, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to put file {ObjectPath} to S3 bucket {BucketName}", objectPath, _bucketName);
                // Optionally re-throw if this is a critical failure, but cache failures are often suppressed
            }
        }

        public async Task<bool> FileExistsAsync(string objectPath, CancellationToken token = default, string? region = null)
        {
            try
            {
                var client = S3StorageHelpers.GetS3Client(_s3StorageClientFactory, region);

                // Check metadata to verify existence
                await client.GetObjectMetadataAsync(_bucketName, objectPath, token);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check existence for file {ObjectPath} in S3 bucket {BucketName}", objectPath, _bucketName);
                return false;
            }
        }

        public string? GeneratePresignedUrl(string fileId, int expiresInSeconds, string? region = null)
        {
            var client = S3StorageHelpers.GetS3Client(_s3StorageClientFactory, region);
            return S3StorageHelpers.GeneratePresignedUrl(client, _bucketName, fileId, expiresInSeconds, _logger);
        }
    }
}