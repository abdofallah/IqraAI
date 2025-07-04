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

        public TTSAudioCacheStorageRepository(ILogger<TTSAudioCacheStorageRepository> logger, IMinioClient minioClient)
        {
            _logger = logger;
            _minioClient = minioClient;
        }

        public async Task<ReadOnlyMemory<byte>> GetFileAsByteArrayAsync(string bucketName, string objectPath, CancellationToken token = default)
        {
            try
            {
                var stream = new MemoryStream();
                var args = new GetObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectPath)
                    .WithCallbackStream(async (s, ct) => await s.CopyToAsync(stream, ct));

                await _minioClient.GetObjectAsync(args, token);
                stream.Position = 0; // Reset position for reading
                return new ReadOnlyMemory<byte>(stream.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get file {ObjectPath} from Minio bucket {BucketName}", objectPath, bucketName);
                return ReadOnlyMemory<byte>.Empty;
            }
        }

        public async Task PutFileAsByteDataAsync(string bucketName, string objectPath, ReadOnlyMemory<byte> fileBytes, Dictionary<string, string> metaData, CancellationToken token = default)
        {
            try
            {
                using var fileStream = fileBytes.AsStream();
                var args = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectPath)
                    .WithStreamData(fileStream)
                    .WithObjectSize(fileStream.Length)
                    .WithContentType("audio/pcm") // A more specific content type
                    .WithHeaders(metaData);

                await _minioClient.PutObjectAsync(args, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to put file {ObjectPath} to Minio bucket {BucketName}", objectPath, bucketName);
                // Optionally re-throw if this is a critical failure
            }
        }
    }
}
