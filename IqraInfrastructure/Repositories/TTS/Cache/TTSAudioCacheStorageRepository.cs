using CommunityToolkit.HighPerformance;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

namespace IqraInfrastructure.Repositories.TTS.Cache
{
    public class TTSAudioCacheStorageRepository
    {
        private readonly ILogger<TTSAudioCacheStorageRepository> _logger;
        private readonly IMinioClient _minioClient;
        public string _bucketName;

        public TTSAudioCacheStorageRepository(ILogger<TTSAudioCacheStorageRepository> logger, IMinioClient minioClient, string bucketName)
        {
            _logger = logger;
            _minioClient = minioClient;
            _bucketName = bucketName;

            bool bucketExists = _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName)).GetAwaiter().GetResult();
            if (!bucketExists)
            {
                throw new ArgumentException("Bucket " + bucketName + " does not exist");
            }
        }

        public async Task<ReadOnlyMemory<byte>> GetFileAsByteArrayAsync(string objectPath, CancellationToken token = default)
        {
            try
            {
                var stream = new MemoryStream();
                var args = new GetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectPath)
                    .WithCallbackStream(async (s, ct) => await s.CopyToAsync(stream, ct));

                await _minioClient.GetObjectAsync(args, token);
                stream.Position = 0; // Reset position for reading
                return new ReadOnlyMemory<byte>(stream.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get file {ObjectPath} from Minio bucket {BucketName}", objectPath, _bucketName);
                return ReadOnlyMemory<byte>.Empty;
            }
        }

        public async Task PutFileAsByteDataAsync(string objectPath, ReadOnlyMemory<byte> fileBytes, Dictionary<string, string> metaData, CancellationToken token = default)
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

                await _minioClient.PutObjectAsync(args, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to put file {ObjectPath} to Minio bucket {BucketName}", objectPath, _bucketName);
                // Optionally re-throw if this is a critical failure
            }
        }
    }
}
