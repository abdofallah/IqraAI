using Minio;
using CommunityToolkit.HighPerformance;
using Minio.DataModel.Args;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Repositories.Business
{
    public class BusinessLogoRepository
    {
        private readonly ILogger<BusinessLogoRepository> _logger;

        private IMinioClient MinioClient;
        public string BucketName;

        public BusinessLogoRepository(ILogger<BusinessLogoRepository> logger, IMinioClient client, string bucketName)
        {
            _logger = logger;

            MinioClient = client;
            BucketName = bucketName;

            bool bucketExists = MinioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName)).GetAwaiter().GetResult();
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

            await MinioClient.PutObjectAsync(args);
        }

        public async Task<MemoryStream> GetFileAtPath(string fileId, string filePath)
        {
            MemoryStream stream = new MemoryStream();

            var args = new GetObjectArgs()
                .WithBucket(BucketName)
                .WithObject(fileId)
                .WithFile(filePath);

            await MinioClient.GetObjectAsync(args);

            return stream;
        }

        public async Task<bool> FileExists(string fileId)
        {
            try
            {
                var args = new StatObjectArgs()
                    .WithBucket(BucketName)
                    .WithObject(fileId);

                await MinioClient.StatObjectAsync(args);
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

            await MinioClient.GetObjectAsync(args);

            return stream;
        }

        public async Task<ReadOnlyMemory<byte>> GetFileAsByteArray(string fileId)
        {
            return new ReadOnlyMemory<byte>((await GetFileAsMemoryStream(fileId)).ToArray());
        }
    }
}
