using CommunityToolkit.HighPerformance;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.ApiEndpoints;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.Exceptions;
using System.Threading;

namespace IqraInfrastructure.Repositories.Conversation
{
    public class ConversationAudioRepository
    {
        private readonly IMinioClient _minioClient;
        private readonly string _bucketName;
        private readonly ILogger<ConversationAudioRepository> _logger;

        public ConversationAudioRepository(
            string endpoint,
            int port,
            string accessKey,
            string secretKey,
            string bucketName,
            bool isSecure,
            ILogger<ConversationAudioRepository> logger)
        {
            _bucketName = bucketName;
            _logger = logger;

            _minioClient = new MinioClient()
                .WithEndpoint(endpoint, port)
                .WithCredentials(accessKey, secretKey)
                .WithSSL(isSecure)
                .Build();

            // Ensure the bucket exists
            EnsureBucketExistsAsync().GetAwaiter().GetResult();
        }

        private async Task EnsureBucketExistsAsync()
        {
            try
            {
                bool bucketExists = await _minioClient.BucketExistsAsync(
                    new BucketExistsArgs().WithBucket(_bucketName));

                if (!bucketExists)
                {
                    await _minioClient.MakeBucketAsync(
                        new MakeBucketArgs().WithBucket(_bucketName));

                    _logger.LogInformation("Created conversation audio bucket: {BucketName}", _bucketName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring bucket exists: {BucketName}", _bucketName);
                throw;
            }
        }

        public async Task<bool> StoreAudioAsync(string reference, byte[] audioData, Dictionary<string, string>? metadata = null)
        {
            try
            {
                // Ensure the reference is valid
                if (string.IsNullOrEmpty(reference) || reference.Contains(".."))
                {
                    _logger.LogWarning("Invalid audio reference: {Reference}", reference);
                    return false;
                }

                // Create default metadata if not provided
                metadata ??= new Dictionary<string, string>();

                // Add timestamp
                metadata["timestamp"] = DateTime.UtcNow.ToString("o");
                metadata["size"] = audioData.Length.ToString();

                // Store the audio data
                using var audioStream = audioData.AsMemory().AsStream();

                var args = new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(reference)
                    .WithStreamData(audioStream)
                    .WithObjectSize(audioData.Length)
                    .WithContentType("audio/wav") // Adjust content type as needed
                    .WithHeaders(metadata);

                await _minioClient.PutObjectAsync(args);

                _logger.LogDebug("Stored audio with reference {Reference}, size {Size}", reference, audioData.Length);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing audio for reference {Reference}", reference);
                return false;
            }
        }

        public async Task<byte[]?> RetrieveAudioAsync(string reference)
        {
            // Ensure the reference is valid
            if (string.IsNullOrEmpty(reference) || reference.Contains(".."))
            {
                _logger.LogWarning("Invalid audio reference: {Reference}", reference);
                return null;
            }

            ObjectStat? stat = null;
            MemoryStream? memoryStream = null;
            byte[]? data = null;

            try
            {
                var statArgs = new StatObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(reference);
                stat = await _minioClient.StatObjectAsync(statArgs);

                if (stat.Size == 0)
                {
                    return Array.Empty<byte>();
                }

                int initialCapacity = (stat != null && stat.Size > 0) ? (int)stat.Size : 81920;
                memoryStream = new MemoryStream(initialCapacity);

                var args = new GetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(reference)
                    .WithCallbackStream(stream =>
                    {
                        try
                        {
                            stream.CopyTo(memoryStream);
                        }
                        catch (Exception copyEx)
                        {
                            _logger.LogError(copyEx, "Error copying stream within callback for {Reference}", reference);
                            throw;
                        }
                    });

                ObjectStat resultStat = await _minioClient.GetObjectAsync(args);
                data = memoryStream.ToArray();

                if (data != null)
                {
                    return data;
                }
                else
                {
                    return null;
                }
            }
            catch (ObjectNotFoundException)
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

        public async Task<bool> DeleteAudioAsync(string reference)
        {
            try
            {
                // Ensure the reference is valid
                if (string.IsNullOrEmpty(reference) || reference.Contains(".."))
                {
                    _logger.LogWarning("Invalid audio reference: {Reference}", reference);
                    return false;
                }

                await _minioClient.RemoveObjectAsync(
                    new RemoveObjectArgs().WithBucket(_bucketName).WithObject(reference));

                _logger.LogDebug("Deleted audio with reference {Reference}", reference);
                return true;
            }
            catch (ObjectNotFoundException)
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

        public async Task<List<string>> ListAudioForConversationAsync(string conversationId)
        {
            try
            {
                var items = new List<string>();
                var prefix = $"{conversationId}/";

                var listArgs = new ListObjectsArgs()
                    .WithBucket(_bucketName)
                    .WithPrefix(prefix)
                    .WithRecursive(true);

                var observable = _minioClient.ListObjectsEnumAsync(listArgs);

                await foreach (var item in observable)
                {
                    items.Add(item.Key);
                }

                _logger.LogDebug("Listed {Count} audio files for conversation {ConversationId}",
                    items.Count, conversationId);

                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing audio for conversation {ConversationId}", conversationId);
                return new List<string>();
            }
        }

        public async Task<Dictionary<string, string>?> GetAudioMetadataAsync(string reference)
        {
            try
            {
                // Ensure the reference is valid
                if (string.IsNullOrEmpty(reference) || reference.Contains(".."))
                {
                    _logger.LogWarning("Invalid audio reference: {Reference}", reference);
                    return null;
                }

                // Get the object stat to retrieve metadata
                var statArgs = new StatObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(reference);

                var objectStat = await _minioClient.StatObjectAsync(statArgs);

                if (objectStat.MetaData == null || !objectStat.MetaData.Any())
                {
                    return new Dictionary<string, string>();
                }

                return objectStat.MetaData.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value,
                    StringComparer.OrdinalIgnoreCase);
            }
            catch (ObjectNotFoundException)
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
    }
}