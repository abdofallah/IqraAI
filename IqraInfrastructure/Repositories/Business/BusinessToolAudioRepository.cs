using Amazon.S3;
using Amazon.S3.Model;
using CommunityToolkit.HighPerformance;
using IqraCore.Constants;
using IqraInfrastructure.Repositories.S3Storage;
using Microsoft.Extensions.Logging;
using System.Net;

namespace IqraInfrastructure.Repositories.Business
{
    public class BusinessToolAudioRepository
    {
        private readonly ILogger<BusinessToolAudioRepository> _logger;
        private readonly S3StorageClientFactory _s3StorageClientFactory;
        public string _bucketName;

        public BusinessToolAudioRepository(ILogger<BusinessToolAudioRepository> logger, S3StorageClientFactory clientFactory)
        {
            _logger = logger;
            _s3StorageClientFactory = clientFactory;
            _bucketName = S3StorageBucketConstants.BusinessToolAudioRepositoryBucketName;
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
                };

                if (metaData != null)
                {
                    foreach (var kvp in metaData)
                    {
                        request.Metadata.Add(kvp.Key, kvp.Value);
                    }
                }

                var client = S3StorageHelpers.GetDefaultS3Client(_s3StorageClientFactory);
                await client.PutObjectAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error putting business tool audio stream {FileId}", fileId);
                throw;
            }
        }

        public async Task<MemoryStream> GetFileAtPath(string fileId, string filePath)
        {
            try
            {
                var client = S3StorageHelpers.GetDefaultS3Client(_s3StorageClientFactory);

                using var response = await client.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = fileId
                });

                var memoryStream = new MemoryStream();
                await response.ResponseStream.CopyToAsync(memoryStream);

                // Rewind to write to disk
                memoryStream.Position = 0;

                // Ensure directory exists
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Write to file system
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
                _logger.LogError(ex, "Error getting business tool audio {FileId} at path {FilePath}", fileId, filePath);
                throw;
            }
        }

        public async Task<bool> FileExists(string fileId)
        {
            try
            {
                var client = S3StorageHelpers.GetDefaultS3Client(_s3StorageClientFactory);

                // AWS S3 uses GetObjectMetadata (HeadObject) to check existence
                await client.GetObjectMetadataAsync(_bucketName, fileId);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence for business tool audio {FileId}", fileId);
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
                _logger.LogError(ex, "Error getting business tool audio stream {FileId}", fileId);
                throw;
            }
        }

        public async Task<ReadOnlyMemory<byte>> GetFileAsByteArray(string fileId)
        {
            using var stream = await GetFileAsMemoryStream(fileId);
            return new ReadOnlyMemory<byte>(stream.ToArray());
        }

        public string? GeneratePresignedUrl(string fileId, int expiresInSeconds)
        {
            var client = S3StorageHelpers.GetDefaultS3Client(_s3StorageClientFactory);
            return S3StorageHelpers.GeneratePresignedUrl(client, _bucketName, fileId, expiresInSeconds, _logger);
        }
    }
}