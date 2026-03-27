using Amazon.S3;
using Amazon.S3.Model;
using CommunityToolkit.HighPerformance;
using IqraCore.Constants;
using IqraInfrastructure.Repositories.S3Storage;
using Microsoft.Extensions.Logging;
using System.Net;

namespace IqraInfrastructure.Repositories.Business
{
    public class BusinessAgentAudioRepository
    {
        private readonly ILogger<BusinessAgentAudioRepository> _logger;
        private readonly S3StorageClientFactory _s3StorageClientFactory;
        public string _bucketName;

        public BusinessAgentAudioRepository(ILogger<BusinessAgentAudioRepository> logger, S3StorageClientFactory clientFactory)
        {
            _logger = logger;
            _s3StorageClientFactory = clientFactory;
            _bucketName = S3StorageBucketConstants.BusinessAgentAudioRepositoryBucketName;
        }

        public async Task Initalize()
        {
            var client = S3StorageHelpers.GetDefaultS3Client(_s3StorageClientFactory);
            await S3StorageHelpers.EnsureBucketExistsAsync(client, _bucketName, _logger);
        }

        public async Task PutFileAsByteData(string fileId, ReadOnlyMemory<byte> fileBytes, Dictionary<string, string> metaData)
        {
            using var filestream = fileBytes.AsStream();
            await PutFileAsStreamData(fileId, filestream, metaData);
        }

        public async Task PutFileAsStreamData(string fileId, Stream fileStream, Dictionary<string, string> metaData)
        {
            try
            {
                var request = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = fileId,
                    InputStream = fileStream,
                    ContentType = "application/octet-stream"
                    // AutoCloseStream is true by default in AWS SDK, careful if you re-use the stream
                };

                foreach (var kvp in metaData)
                {
                    request.Metadata.Add(kvp.Key, kvp.Value);
                }

                var client = S3StorageHelpers.GetDefaultS3Client(_s3StorageClientFactory);
                await client.PutObjectAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error putting file stream {FileId}", fileId);
                throw;
            }
        }

        public async Task<MemoryStream> GetFileAtPath(string fileId, string filePath)
        {
            try
            {
                var client = S3StorageHelpers.GetDefaultS3Client(_s3StorageClientFactory);

                // Get the object from S3
                using var response = await client.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = fileId
                });

                var memoryStream = new MemoryStream();
                await response.ResponseStream.CopyToAsync(memoryStream);

                // Reset position for the caller
                memoryStream.Position = 0;

                // Save to the specific file path as requested by the method name
                // Ensure directory exists
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    await memoryStream.CopyToAsync(fileStream);
                }

                // Rewind again to return the stream to the caller
                memoryStream.Position = 0;
                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file {FileId} at path {FilePath}", fileId, filePath);
                throw;
            }
        }

        public async Task<bool> FileExists(string fileId)
        {
            try
            {
                var client = S3StorageHelpers.GetDefaultS3Client(_s3StorageClientFactory);

                // In AWS SDK, we use GetObjectMetadata to check existence (HEAD request)
                await client.GetObjectMetadataAsync(_bucketName, fileId);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence for file {FileId}", fileId);
                return false;
            }
        }

        public async Task<MemoryStream> GetFileAsMemoryStream(string fileId)
        {
            try
            {
                var client = S3StorageHelpers.GetDefaultS3Client(_s3StorageClientFactory);

                using var response = await client.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = fileId
                });

                var ms = new MemoryStream();
                await response.ResponseStream.CopyToAsync(ms);
                ms.Position = 0;
                return ms;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file stream {FileId}", fileId);
                throw;
            }
        }

        public async Task<ReadOnlyMemory<byte>> GetFileAsByteArray(string fileId)
        {
            using var stream = await GetFileAsMemoryStream(fileId);
            return new ReadOnlyMemory<byte>(stream.ToArray());
        }

        public async Task<AudioFileResult?> GetFileWithMetadataAsync(string fileId)
        {
            try
            {
                var client = S3StorageHelpers.GetDefaultS3Client(_s3StorageClientFactory);

                // AWS S3 GetObject returns both data and metadata in one call. 
                // No need to call StatObject (HeadObject) separately.
                using var response = await client.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = fileId
                });

                var memoryStream = new MemoryStream();
                await response.ResponseStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                // Extract metadata
                var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var key in response.Metadata.Keys)
                {
                    metadata[key] = response.Metadata[key];
                }

                return new AudioFileResult
                {
                    Data = new ReadOnlyMemory<byte>(memoryStream.ToArray()),
                    Metadata = metadata
                };
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
            {
                _logger.LogWarning("File {FileId} not found in bucket {BucketName}.", fileId, _bucketName);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file {FileId} with metadata", fileId);
                return null;
            }
        }

        public string? GeneratePresignedUrl(string fileId, int expiresInSeconds)
        {
            var client = S3StorageHelpers.GetDefaultS3Client(_s3StorageClientFactory);
            return S3StorageHelpers.GeneratePresignedUrl(client, _bucketName, fileId, expiresInSeconds, _logger);
        }
    }

    public class AudioFileResult
    {
        public ReadOnlyMemory<byte> Data { get; init; }
        public IReadOnlyDictionary<string, string> Metadata { get; init; }
    }
}