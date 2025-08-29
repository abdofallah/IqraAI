using CommunityToolkit.HighPerformance;
using IqraInfrastructure.Repositories.MinIO;
using Microsoft.Extensions.Logging;
using Minio.DataModel;
using Minio.DataModel.Args;

namespace IqraInfrastructure.Repositories.Business
{
    public class BusinessAgentAudioRepository
    {
        private readonly ILogger<BusinessAgentAudioRepository> _logger;

        private MinioPrivatePublicClient _minioClient;
        public string BucketName;

        public BusinessAgentAudioRepository(ILogger<BusinessAgentAudioRepository> logger, MinioPrivatePublicClient client, string bucketName)
        {
            _logger = logger;
            _minioClient = client;
            BucketName = bucketName;

            bool bucketExists = _minioClient.PrivateClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName)).GetAwaiter().GetResult();
            if (!bucketExists)
            {
                throw new ArgumentException("Bucket " + bucketName + " does not exist");
            }
        }

        public async Task PutFileAsByteData(string fileId, ReadOnlyMemory<byte> fileBytes, Dictionary<string, string> metaData)
        {
            using var filestream = fileBytes.AsStream();

            await PutFileAsStreamData(fileId, filestream, metaData);
        }

        public async Task PutFileAsStreamData(string fileId, Stream fileStream, Dictionary<string, string> metaData)
        {
            var args = new PutObjectArgs()
                .WithBucket(BucketName)
                .WithObject(fileId)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType("application/octet-stream")
                .WithHeaders(metaData);

            await _minioClient.PrivateClient.PutObjectAsync(args);
        }

        public async Task<MemoryStream> GetFileAtPath(string fileId, string filePath)
        {
            MemoryStream stream = new MemoryStream();

            var args = new GetObjectArgs()
                .WithBucket(BucketName)
                .WithObject(fileId)
                .WithFile(filePath);

            await _minioClient.PrivateClient.GetObjectAsync(args);

            return stream;
        }

        public async Task<bool> FileExists(string fileId)
        {
            try
            {
                var args = new StatObjectArgs()
                    .WithBucket(BucketName)
                    .WithObject(fileId);

                await _minioClient.PrivateClient.StatObjectAsync(args);
                return true;
            }
            catch (Minio.Exceptions.ObjectNotFoundException)
            {
                return false;
            }
        }

        public async Task<MemoryStream> GetFileAsMemoryStream(string fileId)
        {
            MemoryStream stream = new MemoryStream();

            var args = new GetObjectArgs()
                .WithBucket(BucketName)
                .WithObject(fileId)
                .WithCallbackStream(async s =>
                {
                    await s.CopyToAsync(stream);
                });

            await _minioClient.PrivateClient.GetObjectAsync(args);

            return stream;
        }

        public async Task<ReadOnlyMemory<byte>> GetFileAsByteArray(string fileId)
        {
            return new ReadOnlyMemory<byte>((await GetFileAsMemoryStream(fileId)).ToArray());
        }

        public async Task<AudioFileResult?> GetFileWithMetadataAsync(string fileId)
        {
            try
            {
                // 1. Get Metadata first using StatObject
                ObjectStat? objectStat = null;
                try
                {
                    var statArgs = new StatObjectArgs()
                       .WithBucket(BucketName)
                       .WithObject(fileId);
                    objectStat = await _minioClient.PrivateClient.StatObjectAsync(statArgs).ConfigureAwait(false);
                }
                catch (Minio.Exceptions.ObjectNotFoundException)
                {
                    _logger.LogWarning("File {FileId} not found in bucket {BucketName} when attempting to get metadata.", fileId, BucketName);
                    return null; // File doesn't exist
                }


                // 2. Get the actual file data
                using var memoryStream = new MemoryStream();
                var getArgs = new GetObjectArgs()
                    .WithBucket(BucketName)
                    .WithObject(fileId)
                    .WithCallbackStream(async (stream, cancellationToken) =>
                    {
                        await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
                    });

                await _minioClient.PrivateClient.GetObjectAsync(getArgs).ConfigureAwait(false);

                memoryStream.Position = 0; // Rewind stream

                // Prepare the result
                var metadata = objectStat?.MetaData?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase) // Ensure case-insensitive keys
                               ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                return new AudioFileResult
                {
                    Data = new ReadOnlyMemory<byte>(memoryStream.ToArray()),
                    Metadata = metadata
                };
            }
            catch (Minio.Exceptions.ObjectNotFoundException)
            {
                // Should have been caught by StatObject, but belt-and-suspenders
                _logger.LogWarning("File {FileId} not found in bucket {BucketName} when attempting to get data.", fileId, BucketName);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file {FileId} with metadata from bucket {BucketName}", fileId, BucketName);
                return null; // Or re-throw, depending on desired error handling
            }
        }
    }

    public class AudioFileResult
    {
        public ReadOnlyMemory<byte> Data { get; init; }
        public IReadOnlyDictionary<string, string> Metadata { get; init; }
    }
}
