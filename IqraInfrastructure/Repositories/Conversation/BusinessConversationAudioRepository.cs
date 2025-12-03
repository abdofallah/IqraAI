using Amazon.S3;
using Amazon.S3.Model;
using CommunityToolkit.HighPerformance;
using IqraCore.Entities.App.Configuration;
using IqraInfrastructure.Repositories.S3Storage;
using Microsoft.Extensions.Logging;
using System.Net;

namespace IqraInfrastructure.Repositories.Conversation
{
    public class BusinessConversationAudioRepository
    {
        private readonly S3StorageClientFactory _s3StorageClientFactory;
        private readonly string _bucketName;
        private readonly ILogger<BusinessConversationAudioRepository> _logger;

        public BusinessConversationAudioRepository(ILogger<BusinessConversationAudioRepository> logger, S3StorageClientFactory clientFactory)
        {
            _logger = logger;
            _s3StorageClientFactory = clientFactory;
            _bucketName = S3StorageConfig.BusinessConversationAudioRepositoryBucketName;

            // Ensure the bucket exists in the current region using the Helper
            var client = S3StorageHelpers.GetS3Client(_s3StorageClientFactory, null);
            S3StorageHelpers.EnsureBucketExistsAsync(client, _bucketName, _logger).GetAwaiter().GetResult();
        }

        public async Task<bool> StoreAudioAsync(string reference, byte[] audioData, Dictionary<string, string>? metadata = null, string? region = null)
        {
            try
            {
                if (string.IsNullOrEmpty(reference) || reference.Contains(".."))
                {
                    _logger.LogWarning("Invalid audio reference: {Reference}", reference);
                    return false;
                }

                metadata ??= new Dictionary<string, string>();
                metadata["timestamp"] = DateTime.UtcNow.ToString("o");
                metadata["size"] = audioData.Length.ToString();

                using var audioStream = audioData.AsMemory().AsStream();

                var request = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = reference,
                    InputStream = audioStream,
                    ContentType = "audio/wav"
                };

                foreach (var (key, value) in metadata)
                {
                    request.Metadata.Add(key, value);
                }

                // Use Helper to get client
                var client = S3StorageHelpers.GetS3Client(_s3StorageClientFactory, region);
                await client.PutObjectAsync(request);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing audio for reference {Reference}", reference);
                return false;
            }
        }

        public async Task<byte[]?> RetrieveAudioAsync(string reference, string? region = null)
        {
            if (string.IsNullOrEmpty(reference) || reference.Contains(".."))
            {
                _logger.LogWarning("Invalid audio reference: {Reference}", reference);
                return null;
            }

            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = reference
                };

                var client = S3StorageHelpers.GetS3Client(_s3StorageClientFactory, region);

                using var response = await client.GetObjectAsync(request);
                using var memoryStream = new MemoryStream();

                await response.ResponseStream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
            {
                _logger.LogWarning("Audio not found for reference: {Reference}", reference);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audio for reference {Reference}", reference);
                return null;
            }
        }

        public async Task<bool> DeleteAudioAsync(string reference, string? region = null)
        {
            try
            {
                if (string.IsNullOrEmpty(reference) || reference.Contains(".."))
                {
                    _logger.LogWarning("Invalid audio reference: {Reference}", reference);
                    return false;
                }

                var request = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = reference
                };

                var client = S3StorageHelpers.GetS3Client(_s3StorageClientFactory, region);
                await client.DeleteObjectAsync(request);

                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Audio not found for deletion: {Reference}", reference);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting audio for reference {Reference}", reference);
                return false;
            }
        }

        public async Task<List<string>> ListAudioForConversationAsync(string conversationId, string? region = null)
        {
            try
            {
                var items = new List<string>();
                var prefix = $"{conversationId}/";

                var request = new ListObjectsV2Request
                {
                    BucketName = _bucketName,
                    Prefix = prefix
                };

                ListObjectsV2Response response;
                var client = S3StorageHelpers.GetS3Client(_s3StorageClientFactory, region);

                do
                {
                    response = await client.ListObjectsV2Async(request);

                    foreach (var entry in response.S3Objects)
                    {
                        items.Add(entry.Key);
                    }

                    request.ContinuationToken = response.NextContinuationToken;

                } while (response.IsTruncated.HasValue && response.IsTruncated.Value);

                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing audio for conversation {ConversationId}", conversationId);
                return new List<string>();
            }
        }

        public async Task<Dictionary<string, string>?> GetAudioMetadataAsync(string reference, string? region = null)
        {
            try
            {
                if (string.IsNullOrEmpty(reference) || reference.Contains(".."))
                {
                    _logger.LogWarning("Invalid audio reference: {Reference}", reference);
                    return null;
                }

                var request = new GetObjectMetadataRequest
                {
                    BucketName = _bucketName,
                    Key = reference
                };

                var client = S3StorageHelpers.GetS3Client(_s3StorageClientFactory, region);
                var response = await client.GetObjectMetadataAsync(request);

                if (response.Metadata == null || response.Metadata.Count == 0)
                {
                    return new Dictionary<string, string>();
                }

                var metadataDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var key in response.Metadata.Keys)
                {
                    metadataDict[key] = response.Metadata[key];
                }

                return metadataDict;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
            {
                _logger.LogWarning("Audio not found for metadata retrieval: {Reference}", reference);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metadata for audio reference {Reference}", reference);
                return null;
            }
        }

        public string? GeneratePresignedUrl(string fileId, int expiresInSeconds, string? region = null)
        {
            var client = S3StorageHelpers.GetS3Client(_s3StorageClientFactory, region);
            return S3StorageHelpers.GeneratePresignedUrl(client, _bucketName, fileId, expiresInSeconds, _logger);
        }
    }
}